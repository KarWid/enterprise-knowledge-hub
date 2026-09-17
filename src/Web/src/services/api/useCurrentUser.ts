import { useMemo } from "react";
import { getInitials } from "../../utils/getInitials";
import type { CurrentUserResponse } from "./generated/api";
import { useGetMeQuery } from "./generated/api";

export interface CurrentUser extends CurrentUserResponse {
  initials: string;
}

export function useCurrentUser() {
  const result = useGetMeQuery();

  const data: CurrentUser | undefined = useMemo(() => {
    if (!result.data) {
      return undefined;
    }

    return { ...result.data, initials: getInitials(result.data.name) };
  }, [result.data]);

  return { ...result, data };
}
