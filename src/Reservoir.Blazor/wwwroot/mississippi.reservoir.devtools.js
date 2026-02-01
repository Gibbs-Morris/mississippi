let connection = null;
let unsubscribe = null;

export function connect(options, dotNetRef) {
  if (typeof globalThis.window === 'undefined' || !globalThis.__REDUX_DEVTOOLS_EXTENSION__) {
    return false;
  }

  connection = globalThis.__REDUX_DEVTOOLS_EXTENSION__.connect(options || {});
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
        dotNetRef.invokeMethodAsync('OnDevToolsMessageAsync', JSON.stringify(message));
      } catch {
        // ignore interop failures
      }
    });
  }

  return true;
}

export function init(state) {
  connection?.init?.(state);
}

export function send(action, state) {
  connection?.send?.(action, state);
}

export function disconnect() {
  try {
    connection?.unsubscribe?.();
  } catch {
    // ignore failures
  }

  connection = null;
  unsubscribe = null;
}
