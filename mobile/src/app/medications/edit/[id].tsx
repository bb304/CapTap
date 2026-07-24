import React, { useEffect } from "react";
import { StyleSheet, Text, View } from "react-native";
import { useLocalSearchParams, useRouter } from "expo-router";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button, Chip, Screen, SectionHeader, SkeletonCard } from "@/components/ui";
import { FormTextField } from "@/components/forms/FormTextField";
import { FormError } from "@/components/forms/FormError";
import { ErrorState } from "@/components/common/ErrorState";
import { editMedicationSchema, type EditMedicationFormValues } from "@/components/forms/schemas";
import { useMedication, useUpdateMedication } from "@/hooks/useMedications";
import { spacing, typography } from "@/theme";

const units = ["mg", "mcg", "IU", "ml"] as const;

export default function EditMedicationScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const medicationId = String(id);
  const router = useRouter();

  const medicationQuery = useMedication(medicationId);
  const update = useUpdateMedication(medicationId);

  const { control, handleSubmit, setValue, watch, reset } = useForm<EditMedicationFormValues>({
    resolver: zodResolver(editMedicationSchema),
    defaultValues: {
      name: "",
      dosageAmount: 0,
      dosageUnit: "mg",
      instructions: "",
    },
  });

  // Pre-fill once the medication loads.
  useEffect(() => {
    if (medicationQuery.data) {
      reset({
        name: medicationQuery.data.name,
        dosageAmount: medicationQuery.data.dosageAmount,
        dosageUnit: medicationQuery.data.dosageUnit,
        instructions: medicationQuery.data.instructions ?? "",
      });
    }
  }, [medicationQuery.data, reset]);

  const selectedUnit = watch("dosageUnit");

  const onSubmit = handleSubmit((values) => {
    update.mutate(
      {
        name: values.name,
        dosageAmount: values.dosageAmount,
        dosageUnit: values.dosageUnit,
        instructions: values.instructions?.trim() || null,
      },
      { onSuccess: () => router.back() },
    );
  });

  if (medicationQuery.isLoading) {
    return (
      <Screen>
        <SkeletonCard />
        <SkeletonCard />
      </Screen>
    );
  }

  if (medicationQuery.isError || !medicationQuery.data) {
    return (
      <Screen scroll={false}>
        <ErrorState error={medicationQuery.error} onRetry={() => medicationQuery.refetch()} />
        <Button label="Back" variant="ghost" onPress={() => router.back()} />
      </Screen>
    );
  }

  return (
    <Screen>
      <SectionHeader title="Edit medication" subtitle={medicationQuery.data.name} />

      <FormTextField control={control} name="name" label="Medication name" />
      <FormTextField
        control={control}
        name="dosageAmount"
        label="Dosage amount"
        keyboardType="numeric"
      />

      <Text style={styles.label} maxFontSizeMultiplier={1.4}>
        Unit
      </Text>
      <View style={styles.chips}>
        {units.map((unit) => (
          <Chip
            key={unit}
            label={unit}
            selected={selectedUnit === unit}
            onPress={() => setValue("dosageUnit", unit, { shouldValidate: true })}
          />
        ))}
      </View>

      <FormTextField control={control} name="instructions" label="Instructions (optional)" />

      <FormError error={update.isError ? update.error : undefined} />

      <Button label="Save changes" onPress={onSubmit} loading={update.isPending} />
      <Button label="Cancel" variant="ghost" onPress={() => router.back()} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  label: { ...typography.label },
  chips: { flexDirection: "row", flexWrap: "wrap", gap: spacing.sm },
});
