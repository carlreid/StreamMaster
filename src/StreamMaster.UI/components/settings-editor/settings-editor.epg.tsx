"use client";
import { Box, Heading, SimpleGrid, GridItem } from "@chakra-ui/react";
import type { components } from "../../lib/api.d";
import { SwitchField } from "./settings-editor";

export const EPGSettings = ({
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
			<Heading as="h3" size="md" mb={4}>
				EPG Settings
			</Heading>
			<SimpleGrid columns={{ base: 1 }} gap={4}>
				<GridItem>
					<SwitchField
						label="Auto Set EPG"
						isChecked={settings.autoSetEPG}
						onChange={(checked) => handleChange("autoSetEPG", checked)}
						helperText="Automatically set EPG data for channels."
					/>
				</GridItem>
				<GridItem>
					<SwitchField
						label="Pretty EPG"
						isChecked={settings.prettyEPG}
						onChange={(checked) => handleChange("prettyEPG", checked)}
						helperText="Formats the EPG data for better readability."
					/>
				</GridItem>
				<GridItem>
					<SwitchField
						label="Use Channel Logo For Program Logo"
						isChecked={settings.useChannelLogoForProgramLogo}
						onChange={(checked) =>
							handleChange("useChannelLogoForProgramLogo", checked)
						}
						helperText="Use channel logo for each program's logo."
					/>
				</GridItem>
			</SimpleGrid>
		</Box>
	);
};
