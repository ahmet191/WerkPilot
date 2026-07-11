import axios from 'axios';

// The base instance configuration pointing to the OmniFlow API.
export const api = axios.create({
    baseURL: 'http://localhost:5226/api',
    timeout: 10000,
});

// Request Interceptor: Attach the selected language for dynamic Backend localization
api.interceptors.request.use((config) => {
    // In a real app, this might come from localStorage or AuthContext
    const userLanguage = localStorage.getItem('app-lang') || 'en-US';
    
    config.headers['Accept-Language'] = userLanguage;
    
    // Attach JWT token
    const token = localStorage.getItem('access_token');
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
}, (error) => {
    return Promise.reject(error);
});

// Response Interceptor: Global error handling.
// The API returns RFC 7807 ProblemDetails; `messageKey`, `traceId` and `errors`
// are custom extensions serialized at the top level of the body.
api.interceptors.response.use((response) => {
    return response;
}, (error) => {
    if (error.response?.status === 401) {
        window.dispatchEvent(new CustomEvent('auth-error'));
    }

    const problem = error.response?.data;
    const messageKey = problem?.messageKey || 'api.error.generic';
    const traceId = problem?.traceId;
    const errors = problem?.errors; // field-keyed validation errors, if any

    // Dispatch a custom event to the window so React components (like AppLayout) can listen and translate it
    window.dispatchEvent(new CustomEvent('api-error', {
        detail: { messageKey, traceId, errors, detail: problem?.detail }
    }));

    console.error('API Error:', error.response?.status, problem);
    return Promise.reject(error);
});
