import type { CatalogApi, CatalogType, ParameterDef, VariantInput, VariantRow } from "./catalogApi";

const types: CatalogType[] = [
  { id: "GD1", label: "GD1 Guide", description: "Pipe guide support with dynamic plate and clamp parameters." },
  { id: "RS1", label: "RS1 Rest", description: "Rest support family for common pipe sizes." },
  { id: "HG1", label: "HG1 Hanger", description: "Hanger support template with rod and shoe dimensions." }
];

const paramsByType: Record<string, ParameterDef[]> = {
  GD1: [
    { key: "DN", label: "DN", kind: "text", required: true },
    { key: "A", label: "A", unit: "mm", kind: "number", required: true },
    { key: "P1", label: "P1", unit: "mm", kind: "number" },
    { key: "F2", label: "F2", unit: "mm", kind: "number" }
  ],
  RS1: [
    { key: "DN", label: "DN", kind: "text", required: true },
    { key: "L", label: "L", unit: "mm", kind: "number", required: true },
    { key: "W", label: "W", unit: "mm", kind: "number" },
    { key: "H", label: "H", unit: "mm", kind: "number" }
  ],
  HG1: [
    { key: "DN", label: "DN", kind: "text", required: true },
    { key: "ROD", label: "Rod", unit: "mm", kind: "number", required: true },
    { key: "DROP", label: "Drop", unit: "mm", kind: "number" }
  ]
};

let rowsByType: Record<string, VariantRow[]> = {
  GD1: [
    { pnpId: 101, dn: "80", shortDescription: "GD1 DN80 guide", paramDefinition: { A: 300, P1: 120, F2: 75 } },
    { pnpId: 102, dn: "100", shortDescription: "GD1 DN100 guide", paramDefinition: { A: 360, P1: 140, F2: 90 } }
  ],
  RS1: [
    { pnpId: 201, dn: "50", shortDescription: "RS1 DN50 rest", paramDefinition: { L: 240, W: 160, H: 90 } },
    { pnpId: 202, dn: "150", shortDescription: "RS1 DN150 rest", paramDefinition: { L: 420, W: 260, H: 150 } }
  ],
  HG1: [
    { pnpId: 301, dn: "40", shortDescription: "HG1 DN40 hanger", paramDefinition: { ROD: 16, DROP: 600 } }
  ]
};

const wait = () => new Promise((resolve) => window.setTimeout(resolve, 120));
const clone = <T>(value: T): T => JSON.parse(JSON.stringify(value)) as T;

function toRow(nextId: number, input: VariantInput): VariantRow {
  return { pnpId: nextId, dn: input.dn, shortDescription: input.shortDescription, paramDefinition: { ...input.params } };
}

export const mockCatalogApi: CatalogApi = {
  dtoVersion: "pfs-catalog-v1",
  async listTypes() { await wait(); return clone(types); },
  async listVariants(type) { await wait(); return clone(rowsByType[type] ?? []); },
  async getParamKeys(type) { await wait(); return clone(paramsByType[type] ?? []); },
  async addVariant(type, input) {
    await wait();
    const current = rowsByType[type] ?? [];
    const nextId = Math.max(0, ...Object.values(rowsByType).flat().map((row) => row.pnpId)) + 1;
    rowsByType = { ...rowsByType, [type]: [...current, toRow(nextId, input)] };
    return { ok: true, message: `Mock variant ${nextId} added.` };
  },
  async updateVariant(pnpId, type, input) {
    await wait();
    rowsByType = { ...rowsByType, [type]: (rowsByType[type] ?? []).map((row) => (row.pnpId === pnpId ? toRow(pnpId, input) : row)) };
    return { ok: true, message: `Mock variant ${pnpId} updated.` };
  },
  async deleteVariant(pnpId) {
    await wait();
    rowsByType = Object.fromEntries(Object.entries(rowsByType).map(([type, rows]) => [type, rows.filter((row) => row.pnpId !== pnpId)]));
    return { ok: true, message: `Mock variant ${pnpId} deleted.` };
  },
  async loadAcat() { await wait(); return { ok: true, message: "Mock .acat loaded. P2 will connect the host bridge." }; },
  async preview(request) { await wait(); return { ok: true, message: `Mock preview queued for ${request.type}. P3 will mount the canvas panel.` }; }
};
