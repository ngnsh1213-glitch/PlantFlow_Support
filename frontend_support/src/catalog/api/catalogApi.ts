export type CatalogDtoVersion = "pfs-catalog-v1";

export interface CatalogType {
  id: string;
  label: string;
  description: string;
}

export interface VariantRow {
  pnpId: number;
  dn: string;
  shortDescription: string;
  paramDefinition: Record<string, string | number>;
}

export interface ParameterDef {
  key: string;
  label: string;
  unit?: string;
  kind: "number" | "text";
  required?: boolean;
}

export interface VariantInput {
  dn: string;
  shortDescription: string;
  params: Record<string, string>;
}

export interface AddVariantResult {
  ok: boolean;
  message: string;
}

export interface PreviewRequest {
  type: string;
  params: Record<string, string>;
}

export interface CatalogApi {
  dtoVersion: CatalogDtoVersion;
  listTypes(): Promise<CatalogType[]>;
  listVariants(type: string): Promise<VariantRow[]>;
  getParamKeys(type: string): Promise<ParameterDef[]>;
  addVariant(type: string, input: VariantInput): Promise<AddVariantResult>;
  updateVariant(pnpId: number, type: string, input: VariantInput): Promise<AddVariantResult>;
  deleteVariant(pnpId: number): Promise<AddVariantResult>;
  loadAcat(): Promise<AddVariantResult>;
  preview(request: PreviewRequest): Promise<AddVariantResult>;
}
