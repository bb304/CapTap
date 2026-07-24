import React from "react";
import { Alert, StyleSheet, Text, View } from "react-native";
import { useLocalSearchParams, useRouter } from "expo-router";
import { Nfc } from "lucide-react-native";
import {
  Button,
  Card,
  LoadingSpinner,
  Screen,
  SectionHeader,
  SkeletonCard,
} from "@/components/ui";
import { ErrorState } from "@/components/common/ErrorState";
import { useMedication, useArchiveMedication } from "@/hooks/useMedications";
import { useSchedules } from "@/hooks/useSchedules";
import { useAssignNfcTag, useNfcTags, useUnassignNfcTag } from "@/hooks/useNfc";
import { toUserMessage } from "@/api/errors";
import { NfcService } from "@/services/NfcService";
import { routes } from "@/constants/routes";
import { formatDosage, formatTime } from "@/utils/format";
import { colors, spacing, typography } from "@/theme";

export default function MedicationDetailsScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const medicationId = String(id);
  const router = useRouter();

  const medicationQuery = useMedication(medicationId);
  const schedulesQuery = useSchedules(medicationId);
  const tagsQuery = useNfcTags();
  const assignTag = useAssignNfcTag();
  const unassignTag = useUnassignNfcTag();
  const archive = useArchiveMedication();

  const assignedTag = (tagsQuery.data ?? []).find(
    (tag) => tag.medicationId === medicationId && tag.isAssigned,
  );

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
        <ErrorState
          error={medicationQuery.error}
          onRetry={() => medicationQuery.refetch()}
        />
        <Button label="Back" variant="ghost" onPress={() => router.back()} />
      </Screen>
    );
  }

  const medication = medicationQuery.data;
  const schedules = schedulesQuery.data ?? [];

  const onArchive = () => {
    Alert.alert(
      "Archive medication",
      `Archive ${medication.name}? It will be hidden from your list.`,
      [
        { text: "Cancel", style: "cancel" },
        {
          text: "Archive",
          style: "destructive",
          onPress: () =>
            archive.mutate(medicationId, {
              onSuccess: () => router.back(),
              onError: (err) => Alert.alert("Couldn't archive", toUserMessage(err)),
            }),
        },
      ],
    );
  };

  const onAssign = async () => {
    const availability = await NfcService.checkAvailability();
    if (!availability.supported || !availability.enabled) {
      Alert.alert("NFC unavailable", availability.reason ?? "NFC is not available.");
      return;
    }

    Alert.alert("Assign bottle tag", "Hold your phone near the NFC sticker.", [
      { text: "Cancel", style: "cancel" },
      {
        text: "Scan",
        onPress: async () => {
          const result = await NfcService.scanTag();
          if (!result.ok) {
            if (!result.cancelled) {
              Alert.alert("Scan failed", result.message);
            }
            return;
          }

          assignTag.mutate(
            { medicationId, tagIdentifier: result.tagIdentifier },
            {
              onSuccess: () =>
                Alert.alert("Tag assigned", "This bottle tag is linked to the medication."),
              onError: (err) => Alert.alert("Couldn't assign tag", toUserMessage(err)),
            },
          );
        },
      },
    ]);
  };

  const onUnassign = () => {
    Alert.alert("Unassign tag", "Remove the NFC link from this medication?", [
      { text: "Cancel", style: "cancel" },
      {
        text: "Unassign",
        style: "destructive",
        onPress: () =>
          unassignTag.mutate(
            { medicationId },
            {
              onError: (err) => Alert.alert("Couldn't unassign", toUserMessage(err)),
            },
          ),
      },
    ]);
  };

  return (
    <Screen>
      <SectionHeader
        title={medication.name}
        subtitle={medication.genericName ?? medication.brandName}
      />
      <Card>
        <Text style={styles.meta} maxFontSizeMultiplier={1.5}>
          {formatDosage(medication.dosageAmount, medication.dosageUnit)}
          {medication.form ? ` · ${medication.form}` : ""}
        </Text>
        {medication.instructions ? (
          <Text style={styles.instructions} maxFontSizeMultiplier={1.5}>
            {medication.instructions}
          </Text>
        ) : null}
      </Card>

      <SectionHeader title="Today's schedule" />
      {schedulesQuery.isLoading ? (
        <LoadingSpinner label="Loading schedule" />
      ) : schedules.length === 0 ? (
        <Card>
          <Text style={styles.schedule} maxFontSizeMultiplier={1.5}>
            No schedule yet.
          </Text>
        </Card>
      ) : (
        <Card>
          {schedules.map((schedule) => (
            <Text key={schedule.id} style={styles.schedule} maxFontSizeMultiplier={1.5}>
              {formatTime(schedule.scheduledTime)} · {schedule.doseQuantity} dose
            </Text>
          ))}
        </Card>
      )}

      <Card style={styles.nfc}>
        <Nfc color={colors.primaryDark} size={28} />
        <View style={styles.nfcCopy}>
          <Text style={styles.nfcTitle} maxFontSizeMultiplier={1.4}>
            NFC bottle tag
          </Text>
          <Text style={styles.nfcBody} maxFontSizeMultiplier={1.4}>
            {assignedTag
              ? `Assigned · ${assignedTag.tagIdentifier}`
              : "Link a sticker so tapping the bottle opens confirm-to-log."}
          </Text>
          <View style={styles.nfcActions}>
            {assignedTag ? (
              <Button
                label="Unassign tag"
                variant="secondary"
                onPress={onUnassign}
                loading={unassignTag.isPending}
              />
            ) : (
              <Button
                label="Assign tag"
                onPress={onAssign}
                loading={assignTag.isPending}
              />
            )}
          </View>
        </View>
      </Card>

      <Button
        label="Edit"
        onPress={() => router.push(routes.editMedication(medicationId))}
      />
      <Button
        label="Archive"
        variant="danger"
        onPress={onArchive}
        loading={archive.isPending}
      />
      <Button label="Back" variant="ghost" onPress={() => router.back()} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  meta: { ...typography.subtitle, fontSize: 22 },
  instructions: { ...typography.bodyMuted, marginTop: spacing.sm },
  schedule: { ...typography.body, marginBottom: spacing.sm },
  nfc: { flexDirection: "row", gap: spacing.md, alignItems: "flex-start" },
  nfcCopy: { flex: 1, gap: spacing.xs },
  nfcTitle: { ...typography.label, fontSize: 18 },
  nfcBody: { ...typography.caption },
  nfcActions: { marginTop: spacing.sm },
});
