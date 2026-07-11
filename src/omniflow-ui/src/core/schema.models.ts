export interface UiSchema {
  grid: GridSchema;
  form: FormSchema;
}

export interface GridSchema {
  titleKey: string;
  endpoint: string;
  actions: string[];
  columns: GridColumn[];
}

export interface GridColumn {
  dataIndex: string;
  titleKey: string;
  type: 'string' | 'number' | 'date' | 'tag';
  sortable: boolean;
  ellipsis: boolean;
  width?: number;
}

export interface FormSchema {
  titleKey: string;
  submitEndpoint: string;
  fields: FormField[];
}

export interface FormField {
  name: string;
  labelKey: string;
  type: 'text' | 'select' | 'date' | 'number';
  required: boolean;
  optionsEndpoint?: string;
  options?: FieldOption[];
}

export interface FieldOption {
  label: string;
  value: string;
}
