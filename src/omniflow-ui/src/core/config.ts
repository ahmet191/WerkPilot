// Central endpoint config. Values come from Vite env (.env.development) and
// default to same-origin relative paths, which the dev server proxies to the
// API. No hostname is ever hardcoded in the source.
export const API_URL = import.meta.env.VITE_API_URL ?? '/api';
export const CONNECT_URL = import.meta.env.VITE_CONNECT_URL ?? '/connect';
export const HUBS_URL = import.meta.env.VITE_HUBS_URL ?? '/hubs';

export const TOKEN_KEY = 'access_token';
export const REFRESH_TOKEN_KEY = 'refresh_token';
