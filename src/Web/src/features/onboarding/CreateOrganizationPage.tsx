import { Form, Formik } from "formik";
import { useTranslation } from "react-i18next";
import * as Yup from "yup";
import { Input } from "../../components/ui/Input/Input";
import { Label } from "../../components/ui/Label/Label";
import { LogoutButton } from "../../components/LogoutButton";
import {
  useCreateOrganizationMutation,
  useGetMeQuery,
} from "../../services/api/generated/api";
import styles from "./CreateOrganizationPage.module.less";

interface FormValues {
  name: string;
}

export function CreateOrganizationPage() {
  const { t } = useTranslation();
  const { refetch: refetchMe } = useGetMeQuery();
  const [createOrganization, { isError }] = useCreateOrganizationMutation();

  const validationSchema = Yup.object({
    name: Yup.string()
      .trim()
      .required(t("onboarding.companyNameRequired"))
      .max(100, t("onboarding.companyNameTooLong")),
  });

  async function handleSubmit(values: FormValues) {
    const result = await createOrganization({
      createOrganizationRequest: { name: values.name.trim() },
    });
    if (!("error" in result)) {
      refetchMe();
    }
  }

  return (
    <div className={styles.page}>
      <LogoutButton className={styles.logout} />
      <div className={styles.card}>
        <span className={styles.logo}>{t("app.title")}</span>
        <h1 className={styles.heading}>{t("onboarding.createOrganization")}</h1>
        <p className={styles.subtitle}>
          {t("onboarding.createOrganizationSubtitle")}
        </p>

        <Formik<FormValues>
          initialValues={{ name: "" }}
          validationSchema={validationSchema}
          onSubmit={handleSubmit}
        >
          {({ isSubmitting }) => (
            <Form className={styles.form}>
              <Label htmlFor="name" required>
                {t("onboarding.companyNameLabel")}
              </Label>
              <Input
                name="name"
                id="name"
                placeholder={t("onboarding.companyNamePlaceholder")}
                disabled={isSubmitting}
                autoFocus
                maxLength={100}
              />
              {isError && (
                <p className={styles.error} role="alert">
                  {t("onboarding.createOrganizationError")}
                </p>
              )}
              <button
                type="submit"
                className={styles.button}
                disabled={isSubmitting}
              >
                {isSubmitting
                  ? t("app.pleaseWait")
                  : t("onboarding.createOrganizationButton")}
              </button>
            </Form>
          )}
        </Formik>
      </div>
    </div>
  );
}
