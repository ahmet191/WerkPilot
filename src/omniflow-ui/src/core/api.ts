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
    
    // We could also attach JWT tokens here
    // const token = localStorage.getItem('token');
    // if (token) config.headers.Authorization = `Bearer ${token}`;

    return config;
}, (error) => {
    return Promise.reject(error);
});

// Response Interceptor: Global error handling
api.interceptors.response.use((response) => {
    return response;
}, (error) => {
    // We can show Ant Design notification here based on status codes
    console.error('API Error:', error.response?.status, error.response?.data);
    return Promise.reject(error);
});
