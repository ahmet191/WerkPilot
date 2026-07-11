import { api } from '../core/api';
import type { UiSchema, FormSchema } from '../core/schema.models';

export const SchemaService = {
  getSchema: async (entityType: string): Promise<UiSchema> => {
    const response = await api.get<UiSchema>(`/uischema/${entityType}`);
    return response.data;
  },

  getFormSchema: async (entityType: string): Promise<FormSchema> => {
    const response = await api.get<FormSchema>(`/uischema/forms/${entityType}`);
    return response.data;
  },

  // Generic data fetcher
  fetchGridData: async (endpoint: string): Promise<any[]> => {
    const response = await api.get<any[]>(endpoint);
    return response.data;
  },

  // Generic data submitter
  submitFormData: async (endpoint: string, payload: any): Promise<void> => {
    await api.post(endpoint, payload);
  }
};
