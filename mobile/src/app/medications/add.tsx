import React, { useState } from "react";
import { StyleSheet, Text, View } from "react-native";
import { useRouter } from "expo-router";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { MedicationSearchList } from "@/components/medication/MedicationSearchList";
import { NoResults } from "@/components/common/NoResults";
import {
  Button,
  Chip,
  LoadingSpinner,
  Screen,
  SearchBar,
  SectionHeader,
} from "@/components/ui";
import { FormTextField } from "@/components/forms/FormTextField";
import { FormError } from "@/components/forms/FormError";
import {
  addMedicationSchema,
  type AddMedicationFormValues,
} from "@/components/forms/schemas";
import { medicationApi } from "@/api/medication";
import { scheduleApi } from "@/api/schedule";
import { useMedicationSearch } from "@/hooks/useMedications";
import { queryKeys } from "@/constants/queryKeys";
import { toApiTime } from "@/utils/format";
import { spacing, typography } from "@/theme";

const units = ["mg", "mcg", "IU", "ml"] as const;

export default function AddMedicationScreen() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [query, setQuery] = useState("");
  const [picked, setPicked] = useState(false);

  const search = useMedicationSearch(picked ? "" : query);

  const { control, handleSubmit, setValue, watch } = useForm<AddMedicationFormValues>({
    resolver: zodResolver(addMedicationSchema),
    defaultValues: {
      name: "",
      dosageAmount: 500,
      dosageUnit: "mg",
      scheduleTime: "08:00",
      instructions: "",
    },
  });

  const selectedUnit = watch("dosageUnit");

  const createFlow = useMutation({
    mutationFn: async (values: AddMedicationFormValues) => {
      const medication = await medicationApi.create({
        name: values.name,
        dosageAmount: values.dosageAmount,
        dosageUnit: values.dosageUnit,
        instructions: values.instructions?.trim() || null,
      });
      await scheduleApi.create(medication.id, {
        frequency: "OnceDaily",
        scheduledTime: toApiTime(values.scheduleTime),
        doseQuantity: 1,
      });
      return medication;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.medications });
      queryClient.invalidateQueries({ queryKey: queryKeys.dashboard });
      router.back();
    },
  });

  const onSubmit = handleSubmit((values) => createFlow.mutate(values));

  const results = search.data ?? [];
  const showNoResults =
    !picked && query.trim().length > 1 && !search.isFetching && results.length === 0;

  return (
    <Screen>
      <SectionHeader title="Add medication" subtitle="Search, personalize, save" />
      <SearchBar
        value={query}
        onChangeText={(text) => {
          setQuery(text);
          setPicked(false);
        }}
      />
      {search.isFetching && !picked ? <LoadingSpinner label="Searching" /> : null}
      {showNoResults ? <NoResults query={query} /> : null}
      {!picked && results.length > 0 ? (
        <MedicationSearchList
          results={results.map((r) => ({
            name: r.name,
            brand: r.brand ?? undefined,
            identifier: r.identifier ?? undefined,
          }))}
          onSelect={(item) => {
            setValue("name", item.name, { shouldValidate: true });
            setQuery(item.name);
            setPicked(true);
          }}
        />
      ) : null}

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

      <FormTextField
        control={control}
        name="scheduleTime"
        label="Schedule time (HH:MM)"
        placeholder="08:00"
      />
      <FormTextField
        control={control}
        name="instructions"
        label="Instructions (optional)"
      />

      <FormError error={createFlow.isError ? createFlow.error : undefined} />

      <Button label="Save medication" onPress={onSubmit} loading={createFlow.isPending} />
      <Button label="Cancel" variant="ghost" onPress={() => router.back()} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  label: { ...typography.label },
  chips: { flexDirection: "row", flexWrap: "wrap", gap: spacing.sm },
});
