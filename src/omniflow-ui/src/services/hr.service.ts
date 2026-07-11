import { api } from '../core/api';
import type { DepartmentDto, AssetDto } from '../features/hr/models/hr.models';

export const HrService = {
    getDepartments: async (): Promise<DepartmentDto[]> => {
        const response = await api.get<DepartmentDto[]>('/departments');
        return response.data;
    },
    
    getAssets: async (): Promise<AssetDto[]> => {
        const response = await api.get<AssetDto[]>('/assets');
        return response.data;
    }
};
