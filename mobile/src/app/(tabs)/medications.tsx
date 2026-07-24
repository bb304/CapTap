import React from "react";
import { RefreshControl, StyleSheet, View } from "react-native";
import { useRouter } from "expo-router";
import { MedicationCard } from "@/components/medication/MedicationCard";
import { FloatingActionButton } from "@/components/common/FloatingActionButton";
import { EmptyState, Screen, SectionHeader, SkeletonCard } from "@/components/ui";
import { ErrorState } from "@/components/common/ErrorState";
import { useMedications } from "@/hooks/useMedications";
import { routes } from "@/constants/routes";
import { colors } from "@/theme";

export default function MedicationsScreen() {
  const router = useRouter();
  const { data, isLoading, isError, error, refetch, isRefetching } = useMedications();

  if (isLoading) {
    return (
      <Screen>
        <SectionHeader title="My medications" subtitle="Your personal list" />
        <SkeletonCard />
        <SkeletonCard />
        <SkeletonCard />
      </Screen>
    );
  }

  if (isError) {
    return (
      <Screen scroll={false}>
        <ErrorState error={error} onRetry={() => refetch()} />
      </Screen>
    );
  }

  return (
    <View style={styles.root}>
      <Screen
        refreshControl={
          <RefreshControl
            refreshing={isRefetching}
            onRefresh={() => refetch()}
            tintColor={colors.primary}
          />
        }
      >
        <SectionHeader title="My medications" subtitle="Your personal list" />
        {!data || data.length === 0 ? (
          <EmptyState
            title="No medications yet"
            description="Add your first medication to start tracking."
            actionLabel="Add medication"
            onAction={() => router.push(routes.addMedication)}
          />
        ) : (
          data.map((medication) => (
            <MedicationCard
              key={medication.id}
              medication={medication}
              onPress={() => router.push(routes.medicationDetails(medication.id))}
            />
          ))
        )}
      </Screen>
      <FloatingActionButton onPress={() => router.push(routes.addMedication)} />
    </View>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1 },
});
