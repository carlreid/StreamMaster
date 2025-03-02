"use client";
import { Box, Heading, SimpleGrid, GridItem } from "@chakra-ui/react";
import type { components } from "../../lib/api.d";
import { SwitchField, InputField } from "./settings-editor";

export const BackupSettings = ({
	settings,
	handleChange,
}: {
	settings: components["schemas"]["SettingDto"];
	handleChange: <K extends keyof components["schemas"]["SettingDto"]>(
		key: K,
		value: components["schemas"]["SettingDto"][K],
	) => void;
}) => {
	return (
		<Box borderWidth="1px" borderRadius="md" p={4} boxShadow="sm">
			<SimpleGrid columns={{ base: 1 }} gap={4}>
				<GridItem>
					<SwitchField
						label="Backup Enabled"
						isChecked={settings.backupEnabled}
						onChange={(checked) => handleChange("backupEnabled", checked)}
						helperText="Enable automatic backups."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="Backup Interval"
						type="number"
						value={settings.backupInterval}
						onChange={(value) => handleChange("backupInterval", value)}
						helperText="Interval in minutes between backups."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="Backup Versions To Keep"
						type="number"
						value={settings.backupVersionsToKeep}
						onChange={(value) => handleChange("backupVersionsToKeep", value)}
						helperText="Number of backup versions to keep."
					/>
				</GridItem>
			</SimpleGrid>
		</Box>
	);
};
