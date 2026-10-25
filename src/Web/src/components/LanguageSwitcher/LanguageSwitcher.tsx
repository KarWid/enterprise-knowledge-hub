import { useTranslation } from "react-i18next";
import {
  changeApplicationLanguage,
  type ApplicationLanguage,
} from "../../i18n";
import { LanguageIcon } from "../icons";
import {
  Dropdown,
  type DropdownOption,
} from "../ui/Dropdown/Dropdown";
import styles from "./LanguageSwitcher.module.less";

interface LanguageSwitcherProps {
  className?: string;
}

const languageOptions: DropdownOption[] = [
  {
    value: "en",
    label: "English",
  },
  {
    value: "pl",
    label: "Polski",
  },
];

export function LanguageSwitcher({ className }: LanguageSwitcherProps) {
  const { t, i18n } = useTranslation();
  const selectedLanguage: ApplicationLanguage =
    i18n.resolvedLanguage === "pl" ? "pl" : "en";

  function changeLanguage(language: string) {
    if (language !== "en" && language !== "pl") {
      return;
    }

    void changeApplicationLanguage(language);
  }

  return (
    <Dropdown
      options={languageOptions}
      value={selectedLanguage}
      onChange={changeLanguage}
      ariaLabel={t("language.selectorLabel")}
      title={t("language.selectorLabel")}
      className={`${styles.languageSwitcher}${className ? ` ${className}` : ""}`}
      triggerClassName={styles.trigger}
      triggerContent={<LanguageIcon className={styles.icon} />}
    />
  );
}
