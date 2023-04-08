export const {
  v2Backend,
  v3Backend,
  v4Backend,
  v5Backend,
  mainBackend,
  previewBackend,
  AST_BACKEND: astBackend,
  OAK_BACKEND: oakBackend,
} = await import('/env.js')
  // Avoid crashing the app if the /env.js file is not found
  // The app won't work but at least it won't show as a blank page
  .catch(() => ({}));

const url = new URL(import.meta.url);
// Use the current URL without the path as the baseUrl
url.search = '';
url.hash = '';
url.pathname = '';

export const baseUrl = url.toString();
