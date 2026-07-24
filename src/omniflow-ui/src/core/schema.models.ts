export interface UiSchema {
  grid?: GridSchema;
  form?: FormSchema;
  widgets?: WidgetSchema[];
}

export interface WidgetSchema {
  type: string;
  titleKey: string;
  endpoint: string;
  dataKey: string;
  icon: string;
  color: string;
}

export interface GridSchema {
  titleKey: string;
  endpoint: string;
  actions: string[];
  columns: GridColumn[];
  rowActions?: GridAction[];
}

export interface GridAction {
  labelKey: string;
  method: string;
  endpoint: string;
  color: string;
  /** 'edit' | 'delete' | 'action' — drives which toolbar button is rendered. */
  actionType?: string;
}

export interface GridColumn {
  dataIndex: string;
  titleKey: string;
  type: 'string' | 'number' | 'date' | 'tag';
  sortable: boolean;
  /** Enables the per-column filter dropdown (server-side filtering). */
  filterable?: boolean;
  ellipsis: boolean;
  width?: number;
}

export interface FormSchema {
  titleKey: string;
  submitEndpoint: string;
  method: string;
  fields: FormField[];
  templateEndpoint?: string;
  dynamicSchemaEndpoint?: string;
}

export interface FormField {
  name: string;
  labelKey: string;
  type: 'text' | 'select' | 'date' | 'number';
  required: boolean;
  dataSourceEndpoint?: string;
  regexPattern?: string;
  minLength?: number;
  maxLength?: number;
  options?: FieldOption[];
}

export interface FieldOption {
  label: string;
  value: string;
}
