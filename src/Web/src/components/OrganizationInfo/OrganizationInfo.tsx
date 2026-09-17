import { useEffect, useMemo } from "react";
import { useTranslation } from "react-i18next";
import { BuildingIcon } from "../icons";
import { Dropdown } from "../ui/Dropdown/Dropdown";
import { useGetMeQuery } from "../../services/api/generated/api";
import { useAppDispatch, useAppSelector } from "../../services/hooks";
import {
  selectCurrentOrganizationId,
  setCurrentOrganizationId,
} from "../../services/organization/organizationSlice";
import styles from "./OrganizationInfo.module.less";

export function OrganizationInfo() {
  const { t } = useTranslation();
  const { data } = useGetMeQuery();
  const dispatch = useAppDispatch();
  const currentOrganizationId = useAppSelector(selectCurrentOrganizationId);

  const organizations = useMemo(
    () => (data?.organizations ?? []).filter((org) => !!org.id && !!org.name),
    [data?.organizations],
  );

  useEffect(() => {
    if (organizations.length === 0) {
      return;
    }

    const hasValidSelection = organizations.some(
      (org) => org.id === currentOrganizationId,
    );

    if (!hasValidSelection) {
      dispatch(setCurrentOrganizationId(organizations[0].id!));
    }
  }, [organizations, currentOrganizationId, dispatch]);

  if (organizations.length === 0) {
    return null;
  }

  if (organizations.length === 1) {
    return (
      <div className={styles.singleOrganization} title={organizations[0].name!}>
        <BuildingIcon className={styles.icon} />
        <span className={styles.name}>{organizations[0].name}</span>
      </div>
    );
  }

  const selectedId = organizations.some(
    (org) => org.id === currentOrganizationId,
  )
    ? currentOrganizationId!
    : organizations[0].id!;

  return (
    <div className={styles.wrapper}>
      <BuildingIcon className={styles.icon} />
      <Dropdown
        options={organizations.map((org) => ({
          label: org.name!,
          value: org.id!,
        }))}
        value={selectedId}
        onChange={(newOrganizationId) =>
          dispatch(setCurrentOrganizationId(newOrganizationId))
        }
        ariaLabel={t("organization.selectLabel")}
      />
    </div>
  );
}
