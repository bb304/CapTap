import React, { useState } from "react";
import { Alert, StyleSheet, Text } from "react-native";
import { useLocalSearchParams, useRouter } from "expo-router";
import { Button, Card, Screen, SectionHeader } from "@/components/ui";
import { toUserMessage } from "@/api/errors";
import { useLogMedication } from "@/hooks/useMedicationLogs";
import { routes } from "@/constants/routes";
import { formatDosage, formatTime } from "@/utils/format";
import { colors, spacing, typography } from "@/theme";

export default function NfcConfirmScreen() {
  const router = useRouter();
  const params = useLocalSearchParams<{
    medicationId: string;
    medicationName: string;
    dosageAmount: string;
    dosageUnit: string;
    form?: string;
    scheduleId?: string;
    scheduledTime?: string;
    scheduledDoseTime?: string;
    status?: string;
    alreadyLogged?: string;
    tagIdentifier?: string;
  }>();

  const logMedication = useLogMedication();
  const [confirming, setConfirming] = useState(false);
  const alreadyLogged = params.alreadyLogged === "1";

  const onConfirm = () => {
    if (!params.medicationId || !params.scheduledDoseTime) {
      Alert.alert("Missing dose", "This tag has no active schedule for today.");
      return;
    }

    setConfirming(true);
    logMedication.mutate(
      {
        medicationId: params.medicationId,
        scheduleId: params.scheduleId || undefined,
        scheduledDoseTime: params.scheduledDoseTime,
        loggingMethod: "Nfc",
      },
      {
        onSuccess: () => {
          router.replace(routes.tabs);
        },
        onError: (err) => Alert.alert("Couldn't log dose", toUserMessage(err)),
        onSettled: () => setConfirming(false),
      },
    );
  };

  return (
    <Screen>
      <SectionHeader
        title="Confirm dose"
        subtitle="CapTap recognized your medication. Confirm to log it."
      />
      <Card style={styles.card}>
        <Text style={styles.name} maxFontSizeMultiplier={1.4}>
          {params.medicationName}
        </Text>
        <Text style={styles.meta} maxFontSizeMultiplier={1.4}>
          {formatDosage(Number(params.dosageAmount), params.dosageUnit)}
          {params.form ? ` · ${params.form}` : ""}
        </Text>
        {params.scheduledTime ? (
          <Text style={styles.meta} maxFontSizeMultiplier={1.4}>
            Scheduled {formatTime(params.scheduledTime)}
          </Text>
        ) : null}
        <Text style={styles.status} maxFontSizeMultiplier={1.4}>
          {alreadyLogged
            ? "Already logged today"
            : params.status
              ? `Status: ${params.status}`
              : "Ready to log"}
        </Text>
      </Card>

      <Button
        label={alreadyLogged ? "Already logged" : "Confirm"}
        onPress={onConfirm}
        loading={confirming || logMedication.isPending}
        disabled={alreadyLogged || !params.scheduledDoseTime}
      />
      <Button label="Cancel" variant="ghost" onPress={() => router.back()} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  card: { gap: spacing.sm, marginBottom: spacing.lg },
  name: { ...typography.title, fontSize: 28 },
  meta: { ...typography.body },
  status: { ...typography.bodyMuted, color: colors.primaryDark, marginTop: spacing.sm },
});
