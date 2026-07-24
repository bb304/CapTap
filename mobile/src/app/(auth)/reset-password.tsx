import React from "react";
import { StyleSheet, Text, View } from "react-native";
import { useLocalSearchParams, useRouter } from "expo-router";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button, Screen, SectionHeader } from "@/components/ui";
import { FormTextField } from "@/components/forms/FormTextField";
import { FormError } from "@/components/forms/FormError";
import { resetPasswordSchema, type ResetPasswordFormValues } from "@/components/forms/schemas";
import { useResetPassword } from "@/hooks/useAuthMutations";
import { routes } from "@/constants/routes";
import { spacing, typography } from "@/theme";

export default function ResetPasswordScreen() {
  const router = useRouter();
  const params = useLocalSearchParams<{ token?: string }>();
  const tokenFromLink = typeof params.token === "string" ? params.token : "";
  const resetPassword = useResetPassword();

  const { control, handleSubmit } = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: {
      token: tokenFromLink,
      newPassword: "",
      confirmPassword: "",
    },
  });

  const onSubmit = handleSubmit((values) => {
    resetPassword.mutate(
      { token: values.token, newPassword: values.newPassword },
      {
        onSuccess: () => router.replace(routes.login),
      },
    );
  });

  return (
    <Screen>
      <SectionHeader
        title="Choose a new password"
        subtitle="Use the link from your email, then set a strong password"
      />
      <View style={styles.form}>
        {!tokenFromLink ? (
          <Text style={styles.copy} maxFontSizeMultiplier={1.5}>
            Paste the token from your reset email if the link did not open CapTap automatically.
          </Text>
        ) : null}
        {!tokenFromLink ? (
          <FormTextField
            control={control}
            name="token"
            label="Reset token"
            autoCapitalize="none"
            placeholder="Token from email"
          />
        ) : null}
        <FormTextField
          control={control}
          name="newPassword"
          label="New password"
          secureTextEntry
          autoCapitalize="none"
          placeholder="New password"
        />
        <FormTextField
          control={control}
          name="confirmPassword"
          label="Confirm password"
          secureTextEntry
          autoCapitalize="none"
          placeholder="Confirm password"
        />
        <FormError error={resetPassword.isError ? resetPassword.error : undefined} />
        <Button label="Update password" onPress={onSubmit} loading={resetPassword.isPending} />
        <Button
          label="Back to login"
          variant="ghost"
          onPress={() => router.replace(routes.login)}
        />
      </View>
    </Screen>
  );
}

const styles = StyleSheet.create({
  form: { gap: spacing.lg },
  copy: { ...typography.body },
});
