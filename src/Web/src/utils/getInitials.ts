export function getInitials(name?: string | null): string {
  if (!name) {
    return "?";
  }

  return name
    .split(" ")
    .filter(Boolean)
    .map((part) => part[0])
    .slice(0, 2)
    .join("")
    .toUpperCase();
}
