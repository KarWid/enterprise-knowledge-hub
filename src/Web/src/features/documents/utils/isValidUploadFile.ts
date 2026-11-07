import type { DocumentUploadCapabilitiesResponse } from "../../../services/api/generated/api";

export function isValidUploadFile(
  file: Pick<File, "name" | "size">,
  capabilities: DocumentUploadCapabilitiesResponse,
): boolean {
  if (file.size <= 0 || file.size > capabilities.maxSizeBytes) {
    return false;
  }

  const fileName = file.name.trim().toLowerCase();
  return capabilities.formats.some(format =>
    fileName.endsWith(format.extension.toLowerCase()),
  );
}
