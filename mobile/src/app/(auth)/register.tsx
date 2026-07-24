import React from "react";
import { StyleSheet, View } from "react-native";
import { useRouter } from "expo-router";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button, Screen, SectionHeader } from "@/components/ui";
import { FormTextField } from "@/components/forms/FormTextField";
import { FormError } from "@/components/forms/FormError";
import { registerSchema, type RegisterFormValues } from "@/components/forms/schemas";
import { useRegister } from "@/hooks/useAuthMutations";
import { routes } from "@/constants/routes";
import { spacing } from "@/theme";

export default function RegisterScreen() {
  const router = useRouter();
  const register = useRegister();
  const { control, handleSubmit } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { email: "", password: "", confirmPassword: "" },
  });

  const onSubmit = handleSubmit((values) => {
    register.mutate(values, {
      onSuccess: () => router.replace(routes.tabs),
    });
  });

  return (
    <Screen>
      <SectionHeader title="Create account" subtitle="A calm place for your medications" />
      <View style={styles.form}>
        <FormTextField
          control={control}
          name="email"
          label="Email"
          autoCapitalize="none"
          keyboardType="email-address"
          placeholder="you@example.com"
        />
        <FormTextField control={control} name="password" label="Password" secureTextEntry />
        <FormTextField
          control={control}
          name="confirmPassword"
          label="Confirm password"
          secureTextEntry
        />

        <FormError error={register.isError ? register.error : undefined} />

        <Button label="Create account" onPress={onSubmit} loading={register.isPending} />
        <Button label="Back" variant="ghost" onPress={() => router.back()} />
      </View>
    </Screen>
  );
}

const styles = StyleSheet.create({
  form: { gap: spacing.lg },
});
