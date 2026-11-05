const ANALYTICS_SCHEMA_VERSION = 2;
const MAX_REQUEST_CHARACTERS = 20000;
const DASHBOARD_CACHE_SECONDS = 30;
const ANALYTICS_SHEET_NAMES = Object.freeze({test:'RawEvents_Test', production:'RawEvents_Production'});
const ANALYTICS_LEVELS = ['IntroBox', 'IntroBubble', 'IntroBouncyBall', 'Level01', 'IntroSpider', 'IntroFan', 'FanTutorial', 'Level02'];

// New columns are appended, never inserted into the existing v1 column order.
const ANALYTICS_COLUMNS = [
  'schema_version', 'environment', 'server_timestamp', 'client_timestamp',
  'event_id', 'event_type', 'player_id', 'session_id', 'level_run_id',
  'attempt_id', 'level_name', 'checkpoint_id', 'controlled_form',
  'previous_form', 'death_cause', 'position_x', 'position_y', 'has_position',
  'duration_seconds', 'attempt_active_seconds', 'level_active_seconds',
  'death_count', 'start_reason', 'end_reason', 'result', 'build_version',
  'details', 'map_version', 'entry_reason'
];
const ALLOWED_EVENT_TYPES = new Set([
  'session_start', 'level_start', 'attempt_start', 'checkpoint_reached',
  'form_change', 'form_duration', 'death', 'level_complete', 'level_end', 'analytics_snapshot'
]);

function doGet(e) {
  const parameters = e && e.parameter ? e.parameter : {};
  if (parameters.health === '1' || parameters.format === 'json') {
    return jsonResponse_({ok:true, service:'starformer-analytics', schema_version:2,
      accepted_schema_versions:[1,2], environments:ANALYTICS_SHEET_NAMES,
      levels:ANALYTICS_LEVELS, dashboard:true});
  }
  return HtmlService.createHtmlOutputFromFile('Dashboard').setTitle('StarFormer Playtest Analytics');
}

function doPost(e) {
  const lock = LockService.getScriptLock();
  try {
    const body = e && e.postData && e.postData.contents;
    if (!body || body.length > MAX_REQUEST_CHARACTERS)
      return jsonResponse_({ok:false, error:'invalid_request_size'});
    let payload;
    try { payload = JSON.parse(body); }
    catch (error) { return jsonResponse_({ok:false, error:'invalid_json'}); }
    const error = validatePayload_(payload);
    if (error) return jsonResponse_({ok:false, error:error});

    lock.waitLock(20000);
    const sheet = getAnalyticsSheet_(payload.environment);
    ensureHeader_(sheet);
    if (eventAlreadyExists_(sheet, payload.event_id))
      return jsonResponse_({ok:true, duplicate:true});
    const received = new Date().toISOString();
    const row = ANALYTICS_COLUMNS.map(function (column) {
      if (column === 'server_timestamp') return received;
      if (column === 'map_version') return payload.map_version || (payload.level_name ? 'legacy' : '');
      if (column === 'entry_reason') return payload.entry_reason || (payload.level_name ? 'normal' : '');
      return valueOrEmpty_(payload[column]);
    });
    sheet.appendRow(row);
    return jsonResponse_({ok:true, duplicate:false});
  } catch (error) {
    console.error(error && error.stack ? error.stack : error);
    return jsonResponse_({ok:false, error:'server_error'});
  } finally {
    try { lock.releaseLock(); } catch (ignored) { /* Lock may not be acquired. */ }
  }
}

function validatePayload_(payload) {
  if (!payload || typeof payload !== 'object' || Array.isArray(payload)) return 'invalid_json_object';
  if ([1,2].indexOf(Number(payload.schema_version)) === -1) return 'unsupported_schema_version';
  const strings = ['event_id','event_type','player_id','session_id','client_timestamp','environment','build_version'];
  for (let i=0; i<strings.length; i++) {
    const key = strings[i];
    if (typeof payload[key] !== 'string' || !payload[key].trim()) return 'missing_' + key;
    if (payload[key].length > 256) return 'oversized_' + key;
  }
  if (!ALLOWED_EVENT_TYPES.has(payload.event_type)) return 'invalid_event_type';
  if (!Object.prototype.hasOwnProperty.call(ANALYTICS_SHEET_NAMES,payload.environment)) return 'invalid_environment';
  if (payload.level_name && ANALYTICS_LEVELS.indexOf(payload.level_name) === -1) return 'invalid_level_name';
  if (payload.event_type !== 'session_start' && !payload.level_name) return 'missing_level_name';
  if (Number.isNaN(Date.parse(payload.client_timestamp))) return 'invalid_client_timestamp';
  for (const key of ['map_version','entry_reason']) {
    if (payload[key] !== undefined && (typeof payload[key] !== 'string' || payload[key].length > 128))
      return 'invalid_' + key;
  }
  if (payload.entry_reason && ['normal','return'].indexOf(payload.entry_reason) === -1) return 'invalid_entry_reason';
  if (payload.event_type === 'death' &&
      (payload.has_position !== true || typeof payload.position_x !== 'number' ||
       typeof payload.position_y !== 'number' || !Number.isFinite(payload.position_x) ||
       !Number.isFinite(payload.position_y))) return 'invalid_death_position';
  for (const key of ['duration_seconds','attempt_active_seconds','level_active_seconds','death_count']) {
    if (payload[key] !== undefined && (typeof payload[key] !== 'number' ||
        !Number.isFinite(payload[key]) || payload[key] < 0)) return 'invalid_' + key;
  }
  return '';
}

function getAnalyticsSheet_(environment) {
  const id = PropertiesService.getScriptProperties().getProperty('STARFORMER_ANALYTICS_SHEET_ID');
  const spreadsheet = id ? SpreadsheetApp.openById(id) : SpreadsheetApp.getActiveSpreadsheet();
  if (!spreadsheet) throw new Error('Bind this project to the Sheet or set STARFORMER_ANALYTICS_SHEET_ID.');
  return spreadsheet.getSheetByName(ANALYTICS_SHEET_NAMES[environment]) ||
    spreadsheet.insertSheet(ANALYTICS_SHEET_NAMES[environment]);
}

// Called under the POST/migration lock. Back up v1 before extending its header.
// Existing rows and columns are never cleared or rewritten.
function ensureHeader_(sheet) {
  if (sheet.getLastRow() === 0) {
    if (sheet.getMaxColumns() < ANALYTICS_COLUMNS.length)
      sheet.insertColumnsAfter(sheet.getMaxColumns(), ANALYTICS_COLUMNS.length-sheet.getMaxColumns());
    sheet.getRange(1,1,1,ANALYTICS_COLUMNS.length).setValues([ANALYTICS_COLUMNS]);
    sheet.setFrozenRows(1);
    return;
  }
  const header = sheet.getRange(1,1,1,sheet.getLastColumn()).getValues()[0];
  const legacyCount = ANALYTICS_COLUMNS.length - 2;
  if (header.length !== legacyCount && header.length !== ANALYTICS_COLUMNS.length)
    throw new Error(sheet.getName() + ' has an unexpected header; migration stopped.');
  if (!header.every((name,index) => name === ANALYTICS_COLUMNS[index]))
    throw new Error(sheet.getName() + ' header differs from analytics schema; migration stopped.');
  if (header.length === legacyCount) {
    const backup = sheet.copyTo(sheet.getParent());
    backup.setName(sheet.getName() + '_BackupV1_' + Date.now());
    if (sheet.getMaxColumns() < ANALYTICS_COLUMNS.length)
      sheet.insertColumnsAfter(sheet.getMaxColumns(), ANALYTICS_COLUMNS.length-sheet.getMaxColumns());
    sheet.getRange(1,legacyCount+1,1,2).setValues([['map_version','entry_reason']]);
  }
}

function migrateAnalyticsSheets() {
  const lock = LockService.getScriptLock();
  lock.waitLock(20000);
  try {
    Object.keys(ANALYTICS_SHEET_NAMES).forEach(environment => ensureHeader_(getAnalyticsSheet_(environment)));
    return 'Schema v2 headers ready; v1 data preserved in place and backed up.';
  } finally { lock.releaseLock(); }
}

// Editor verification: reads counts/resources only, without appending events.
function verifyDashboardSetup() {
  const maps = JSON.parse(HtmlService.createHtmlOutputFromFile('MapCatalog').getContent()).maps;
  const result = {schema:2, map_assets:maps.length, levels:ANALYTICS_LEVELS, sources:{}};
  ['test','production'].forEach(environment => {
    const data = getDashboardData({environment:environment});
    result.sources[environment] = {events:data.total_rows,builds:data.builds.length,levels:data.levels.length};
  });
  console.log(JSON.stringify(result));
  return result;
}

function eventAlreadyExists_(sheet,eventId) {
  const last = sheet.getLastRow();
  return last >= 2 && !!sheet.getRange(2,5,last-1,1).createTextFinder(eventId).matchEntireCell(true).findNext();
}
function valueOrEmpty_(value) { return value === undefined || value === null ? '' : value; }
function jsonResponse_(body) {
  return ContentService.createTextOutput(JSON.stringify(body)).setMimeType(ContentService.MimeType.JSON);
}
