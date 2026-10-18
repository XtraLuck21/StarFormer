mergeInto(LibraryManager.library, {
  StarFormerAnalytics_Send: function (endpointPointer, payloadPointer) {
    var endpoint = UTF8ToString(endpointPointer);
    var payload = UTF8ToString(payloadPointer);

    if (!endpoint || !payload) {
      return;
    }

    var queueKey = "starformer.analytics.queue.v1";
    var maxQueueSize = 256;

    var readQueue = function () {
      try {
        var stored = window.localStorage.getItem(queueKey);
        var parsed = stored ? JSON.parse(stored) : [];
        return Array.isArray(parsed) ? parsed : [];
      } catch (error) {
        return [];
      }
    };

    var writeQueue = function (queue) {
      try {
        window.localStorage.setItem(
          queueKey,
          JSON.stringify(queue.slice(-maxQueueSize))
        );
      } catch (error) {
        // Analytics storage must never interrupt gameplay.
      }
    };

    var eventId = "";
    try {
      eventId = JSON.parse(payload).event_id || "";
    } catch (error) {
      return;
    }

    var queue = readQueue();
    var alreadyQueued = queue.some(function (entry) {
      return entry.id === eventId;
    });

    if (!alreadyQueued) {
      queue.push({ id: eventId, endpoint: endpoint, payload: payload });
      writeQueue(queue);
    }

    var flushQueue = function (useBeacon) {
      if (useBeacon) {
        var pending = readQueue();
        var remaining = [];

        pending.forEach(function (entry) {
          var accepted = false;
          try {
            accepted = !!navigator.sendBeacon(entry.endpoint, entry.payload);
          } catch (error) {
            accepted = false;
          }

          if (!accepted) {
            remaining.push(entry);
          }
        });

        writeQueue(remaining);
        return;
      }

      var pending = readQueue();
      pending.forEach(function (entry) {
        if (window.__starFormerAnalyticsInFlight &&
            window.__starFormerAnalyticsInFlight[entry.id]) {
          return;
        }

        window.__starFormerAnalyticsInFlight =
          window.__starFormerAnalyticsInFlight || {};
        window.__starFormerAnalyticsInFlight[entry.id] = true;

        fetch(entry.endpoint, {
          method: "POST",
          mode: "no-cors",
          cache: "no-store",
          keepalive: true,
          headers: { "Content-Type": "text/plain;charset=utf-8" },
          body: entry.payload
        }).then(function () {
          var latest = readQueue().filter(function (candidate) {
            return candidate.id !== entry.id;
          });
          writeQueue(latest);
        }).catch(function () {
          // Keep the event in localStorage so a later event can retry it.
        }).finally(function () {
          delete window.__starFormerAnalyticsInFlight[entry.id];
        });
      });
    };

    if (!window.__starFormerAnalyticsListenersInstalled) {
      window.__starFormerAnalyticsListenersInstalled = true;
      window.addEventListener("pagehide", function () {
        flushQueue(true);
      });
      window.addEventListener("starformer-analytics-flush", function () {
        flushQueue(false);
      });
    }

    flushQueue(false);
  },

  StarFormerAnalytics_Flush: function () {
    try {
      window.dispatchEvent(new Event("starformer-analytics-flush"));
    } catch (error) {
      // Flushing must never interrupt gameplay.
    }
  }
});
