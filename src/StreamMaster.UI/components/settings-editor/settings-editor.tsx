"use client";

import {
	Box,
	Input,
	VStack,
	FieldLabel,
	FieldHelperText,
	Tabs,
} from "@chakra-ui/react";
import { useEffect, useState, useCallback } from "react";
import { useApi, useMutate } from "../../lib/use-api";
import type { components } from "../../lib/api.d";
import { Field } from "../ui/field";
import { Switch } from "../ui/switch";
import { apiClient } from "../../lib/api";
import { toaster } from "../ui/toaster";
import { useDebounce } from "react-use";
import { SDSettingsComponent } from "./settings-editor.schedules-direct";
import { GeneralSettings } from "./settings-editor.general";
import { EPGSettings } from "./settings-editor.epg";
import { BackupSettings } from "./settings-editor.backup";
import { AdvancedSettings } from "./settings-editor.advanced";
import { AuthenticationSettings } from "./settings-editor.authentication";

interface SwitchFieldProps {
	label: string;
	isChecked: boolean | undefined;
	onChange: (checked: boolean) => void;
	helperText?: string;
}

export const SwitchField = ({
	label,
	isChecked,
	onChange,
	helperText,
}: SwitchFieldProps) => {
	return (
		<Field>
			<FieldLabel htmlFor={`switch-${label}`}>{label}</FieldLabel>
			<Switch
				id={`switch-${label}`}
				checked={isChecked}
				onCheckedChange={(e) => onChange(e.checked)}
			/>
			{helperText && <FieldHelperText>{helperText}</FieldHelperText>}
		</Field>
	);
};

interface InputFieldProps<TValue> {
	label: string;
	value: TValue;
	onChange: (value: TValue) => void;
	type?: string;
	helperText?: string;
}

export const InputField = <TValue extends string | number | undefined>({
	label,
	value,
	onChange,
	type = "text",
	helperText,
}: InputFieldProps<TValue>) => {
	return (
		<Field>
			<FieldLabel htmlFor={`input-${label}`}>{label}</FieldLabel>
			<Input
				id={`input-${label}`}
				type={type}
				value={value}
				onChange={(e) => onChange(e.target.value as TValue)}
			/>
			{helperText && <FieldHelperText>{helperText}</FieldHelperText>}
		</Field>
	);
};

export const SettingsEditor = () => {
	const {
		data: settingsData,
		error,
		isLoading,
	} = useApi("/api/settings/getsettings");

	const mutateSettings = useMutate();

	const [settings, setSettings] = useState<
		components["schemas"]["SettingDto"] | undefined
	>(undefined);

	const [pendingSettings, setPendingSettings] = useState<
		components["schemas"]["SettingDto"] | undefined
	>(undefined);

	useEffect(() => {
		if (settingsData && !pendingSettings) {
			setPendingSettings(settingsData);
		}
	}, [settingsData, pendingSettings]);

	useEffect(() => {
		if (settingsData) {
			setSettings(settingsData);
			setPendingSettings(settingsData);
		}
	}, [settingsData]);

	const saveSettings = useCallback(
		async (newSettings: components["schemas"]["SettingDto"]) => {
			try {
				await apiClient.PATCH("/api/settings/updatesetting", {
					body: {
						parameters: newSettings,
					},
				});

				// Revalidate the data to ensure consistency
				await mutateSettings(["/api/settings/getsettings"]);

				toaster.create({
					title: "Settings Updated",
					description: "Settings have been successfully updated.",
					type: "success",
				});
			} catch (error) {
				toaster.create({
					title: "Error",
					description: "An error occurred while updating settings.",
					type: "error",
				});
			}
		},
		[mutateSettings],
	);

	useDebounce(
		() => {
			if (
				pendingSettings &&
				settingsData &&
				JSON.stringify(pendingSettings) !== JSON.stringify(settingsData)
			) {
				saveSettings(pendingSettings);
			}
		},
		500, // 500ms debounce time
		[pendingSettings, settingsData],
	);

	const handleChange = useCallback(
		// biome-ignore lint/suspicious/noExplicitAny: Want to be able to pass any key/value
		(key: keyof components["schemas"]["SettingDto"], value: any) => {
			setPendingSettings((prevSettings) => {
				if (!prevSettings) return prevSettings;
				return { ...prevSettings, [key]: value };
			});
		},
		[],
	);

	const handleSdSettingsChange = useCallback(
		// biome-ignore lint/suspicious/noExplicitAny: Want to be able to pass any key/value
		(key: keyof components["schemas"]["SDSettings"], value: any) => {
			setPendingSettings((prevSettings) => {
				if (!prevSettings || !prevSettings.sdSettings) return prevSettings;
				return {
					...prevSettings,
					sdSettings: { ...prevSettings.sdSettings, [key]: value },
				};
			});
		},
		[],
	);

	if (isLoading && !pendingSettings) {
		return <Box>Loading...</Box>;
	}

	if (error) {
		return <Box color="red">Error loading settings</Box>;
	}

	if (!pendingSettings || !settings) {
		return <Box>No settings data available.</Box>;
	}

	const sdSettings = pendingSettings?.sdSettings || {};

	return (
		<VStack align="stretch" gap={8}>
			<Tabs.Root
				defaultValue="general"
				display="flex"
				flexDirection="column"
				flex="1"
				overflow="hidden"
			>
				<Box flexShrink={0}>
					<Tabs.List>
						<Tabs.Trigger value="general">General</Tabs.Trigger>
						<Tabs.Trigger value="epg">EPG</Tabs.Trigger>
						<Tabs.Trigger value="backup">Backup</Tabs.Trigger>
						<Tabs.Trigger value="advanced">Advanced</Tabs.Trigger>
						<Tabs.Trigger value="schedulesDirect">
							Schedules Direct
						</Tabs.Trigger>
						<Tabs.Trigger value="authentication">Authentication</Tabs.Trigger>
						<Tabs.Indicator />
					</Tabs.List>
				</Box>

				<Box flex="1" overflow="hidden" pr={2}>
					<Tabs.Content value="general" height="100%">
						<GeneralSettings
							settings={pendingSettings}
							handleChange={handleChange}
						/>
					</Tabs.Content>
					<Tabs.Content value="epg" height="100%">
						<EPGSettings
							settings={pendingSettings}
							handleChange={handleChange}
						/>
					</Tabs.Content>
					<Tabs.Content value="backup" height="100%">
						<BackupSettings
							settings={pendingSettings}
							handleChange={handleChange}
						/>
					</Tabs.Content>
					<Tabs.Content value="advanced" height="100%">
						<AdvancedSettings
							settings={pendingSettings}
							handleChange={handleChange}
						/>
					</Tabs.Content>
					<Tabs.Content value="schedulesDirect" height="100%">
						<SDSettingsComponent
							settings={sdSettings}
							handleChange={handleSdSettingsChange}
						/>
					</Tabs.Content>
					<Tabs.Content value="authentication" height="100%">
						<AuthenticationSettings
							settings={pendingSettings}
							handleChange={handleChange}
						/>
					</Tabs.Content>
				</Box>
			</Tabs.Root>
		</VStack>
	);
};
