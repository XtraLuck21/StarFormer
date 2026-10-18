const ANALYTICS_SCHEMA_VERSION = 1;
const MAX_REQUEST_CHARACTERS = 20000;

const ANALYTICS_SHEET_NAMES = Object.freeze({
  test: 'RawEvents_Test',
  production: 'RawEvents_Production'
});

const ANALYTICS_COLUMNS = [
  'schema_version',
  'environment',
  'server_timestamp',
  'client_timestamp',
  'event_id',
  'event_type',
  'player_id',
  'session_id',
  'level_run_id',
  'attempt_id',
  'level_name',
  'checkpoint_id',
  'controlled_form',
  'previous_form',
  'death_cause',
  'position_x',
  'position_y',
  'has_position',
  'duration_seconds',
  'attempt_active_seconds',
  'level_active_seconds',
  'death_count',
  'start_reason',
  'end_reason',
  'result',
  'build_version',
  'details'
];

const ALLOWED_EVENT_TYPES = new Set([
  'session_start',
  'level_start',
  'attempt_start',
  'checkpoint_reached',
  'form_change',
  'form_duration',
  'death',
  'level_complete',
  'level_end',
  'analytics_snapshot'
]);

function doGet() {
  return jsonResponse_({
    ok: true,
    service: 'starformer-analytics',
    schema_version: ANALYTICS_SCHEMA_VERSION,
    environments: ANALYTICS_SHEET_NAMES
  });
}

function doPost(e) {
  const lock = LockService.getScriptLock();

  try {
    if (!e || !e.postData || typeof e.postData.contents !== 'string') {
      return jsonResponse_({ ok: false, error: 'missing_body' });
    }

    const rawBody = e.postData.contents;
    if (rawBody.length === 0 || rawBody.length > MAX_REQUEST_CHARACTERS) {
      return jsonResponse_({ ok: false, error: 'invalid_body_size' });
    }

    const payload = JSON.parse(rawBody);
    const validationError = validatePayload_(payload);
    if (validationError) {
      return jsonResponse_({ ok: false, error: validationError });
    }

    if (!lock.tryLock(10000)) {
      return jsonResponse_({ ok: false, error: 'write_lock_timeout' });
    }

    const sheet = getAnalyticsSheet_(payload.environment);
    ensureHeader_(sheet);

    if (eventAlreadyExists_(sheet, payload.event_id)) {
      return jsonResponse_({ ok: true, duplicate: true });
    }

    const serverTimestamp = new Date().toISOString();
    const row = ANALYTICS_COLUMNS.map(function (column) {
      if (column === 'server_timestamp') {
        return serverTimestamp;
      }
      return valueOrEmpty_(payload[column]);
    });

    sheet.appendRow(row);
    return jsonResponse_({ ok: true, duplicate: false });
  } catch (error) {
    console.error(error && error.stack ? error.stack : error);
    return jsonResponse_({ ok: false, error: 'server_error' });
  } finally {
    try {
      lock.releaseLock();
    } catch (ignored) {
      // The lock may not have been acquired.
    }
  }
}

function validatePayload_(payload) {
  if (!payload || typeof payload !== 'object' || Array.isArray(payload)) {
    return 'invalid_json_object';
  }

  if (Number(payload.schema_version) !== ANALYTICS_SCHEMA_VERSION) {
    return 'unsupported_schema_version';
  }

  const requiredStrings = [
    'event_id',
    'event_type',
    'player_id',
    'session_id',
    'client_timestamp',
    'environment',
    'build_version'
  ];

  for (let i = 0; i < requiredStrings.length; i++) {
    const key = requiredStrings[i];
    if (typeof payload[key] !== 'string' || payload[key].trim() === '') {
      return 'missing_' + key;
    }
    if (payload[key].length > 256) {
      return 'oversized_' + key;
    }
  }

  if (!ALLOWED_EVENT_TYPES.has(payload.event_type)) {
    return 'invalid_event_type';
  }

  if (!Object.prototype.hasOwnProperty.call(
      ANALYTICS_SHEET_NAMES,
      payload.environment)) {
    return 'invalid_environment';
  }

  if (payload.level_name &&
      payload.level_name !== 'Level01' &&
      payload.level_name !== 'Level02') {
    return 'invalid_level_name';
  }

  return '';
}

function getAnalyticsSheet_(environment) {
  const spreadsheetId = PropertiesService.getScriptProperties()
    .getProperty('STARFORMER_ANALYTICS_SHEET_ID');
  const spreadsheet = spreadsheetId
    ? SpreadsheetApp.openById(spreadsheetId)
    : SpreadsheetApp.getActiveSpreadsheet();

  if (!spreadsheet) {
    throw new Error(
      'No spreadsheet is available. Bind this script to the target Sheet or ' +
      'set STARFORMER_ANALYTICS_SHEET_ID in Script Properties.'
    );
  }

  const sheetName = ANALYTICS_SHEET_NAMES[environment];
  return spreadsheet.getSheetByName(sheetName) ||
    spreadsheet.insertSheet(sheetName);
}

function ensureHeader_(sheet) {
  if (sheet.getLastRow() === 0) {
    sheet.appendRow(ANALYTICS_COLUMNS);
    sheet.setFrozenRows(1);
    return;
  }

  const existingHeader = sheet
    .getRange(1, 1, 1, ANALYTICS_COLUMNS.length)
    .getValues()[0];
  const matches = ANALYTICS_COLUMNS.every(function (column, index) {
    return existingHeader[index] === column;
  });

  if (!matches) {
    throw new Error(
      sheet.getName() +
      ' header does not match the expected analytics schema.'
    );
  }
}

function eventAlreadyExists_(sheet, eventId) {
  const lastRow = sheet.getLastRow();
  if (lastRow < 2) {
    return false;
  }

  const eventIdColumn = ANALYTICS_COLUMNS.indexOf('event_id') + 1;
  const match = sheet
    .getRange(2, eventIdColumn, lastRow - 1, 1)
    .createTextFinder(eventId)
    .matchEntireCell(true)
    .findNext();
  return match !== null;
}

function valueOrEmpty_(value) {
  return value === undefined || value === null ? '' : value;
}

function jsonResponse_(body) {
  return ContentService
    .createTextOutput(JSON.stringify(body))
    .setMimeType(ContentService.MimeType.JSON);
}
