export interface DepartmentDto {
    id: string;
    name: string;
    managerId: string | null;
    parentDepartmentId: string | null;
}

export interface AssetDto {
    id: string;
    assetTag: string;
    serialNumber: string;
    model: string;
    category: string;
    status: string;
    assignedToUserId: string | null;
}
