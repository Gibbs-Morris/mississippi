let connection = null;
let unsubscribe = null;

export function connect(options, dotNetRef) {
  if (typeof window === 'undefined' || !window.__REDUX_DEVTOOLS_EXTENSION__) {
    return false;
  }

  connection = window.__REDUX_DEVTOOLS_EXTENSION__.connect(options || {});
  if (!connection) {
    return false;
  }

  if (unsubscribe) {
    try {
      unsubscribe();
    } catch {
      // ignore unsubscribe failures
    }
  }

  if (dotNetRef) {
    unsubscribe = connection.subscribe((message) => {
      try {
        dotNetRef.invokeMethodAsync('OnDevToolsMessage', JSON.stringify(message));
      } catch {
        // ignore interop failures
      }
    });
  }

  return true;
}

export function init(state) {
  if (connection && connection.init) {
    connection.init(state);
  }
}

export function send(action, state) {
  if (connection && connection.send) {
    connection.send(action, state);
  }
}

export function disconnect() {
  if (connection && connection.unsubscribe) {
    try {
      connection.unsubscribe();
    } catch {
      // ignore failures
    }
  }

  connection = null;
  unsubscribe = null;
}
