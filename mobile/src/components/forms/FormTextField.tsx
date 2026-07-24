import React from "react";
import { Control, Controller, FieldPath, FieldValues } from "react-hook-form";
import { Input } from "@/components/ui";

type FormTextFieldProps<T extends FieldValues> = {
  control: Control<T>;
  name: FieldPath<T>;
  label: string;
  secureTextEntry?: boolean;
  placeholder?: string;
  autoCapitalize?: "none" | "sentences" | "words" | "characters";
  keyboardType?: "default" | "email-address" | "numeric" | "number-pad";
};

export function FormTextField<T extends FieldValues>({
  control,
  name,
  label,
  secureTextEntry,
  placeholder,
  autoCapitalize,
  keyboardType,
}: FormTextFieldProps<T>) {
  return (
    <Controller
      control={control}
      name={name}
      render={({ field: { onChange, onBlur, value }, fieldState: { error } }) => (
        <Input
          label={label}
          value={value?.toString() ?? ""}
          onChangeText={(text) => {
            if (keyboardType === "numeric" || keyboardType === "number-pad") {
              const parsed = Number(text);
              onChange(Number.isFinite(parsed) ? parsed : 0);
              return;
            }
            onChange(text);
          }}
          onBlur={onBlur}
          error={error?.message}
          secureTextEntry={secureTextEntry}
          placeholder={placeholder}
          autoCapitalize={autoCapitalize}
          keyboardType={keyboardType}
        />
      )}
    />
  );
}
