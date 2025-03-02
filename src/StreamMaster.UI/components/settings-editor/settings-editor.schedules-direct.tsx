"use client";
import {
	Box,
	Heading,
	SimpleGrid,
	GridItem,
	Stack,
	createListCollection,
} from "@chakra-ui/react";
import type { components } from "../../lib/api.d";
import { InputField, SwitchField } from "./settings-editor";
import {
	SelectContent,
	SelectItem,
	SelectLabel,
	SelectRoot,
	SelectTrigger,
	SelectValueText,
} from "../ui/select";

const logoStyleOptions = createListCollection({
	items: [
		{ value: "DARK", label: "Dark" },
		{ value: "GRAY", label: "Gray" },
		{ value: "LIGHT", label: "Light" },
		{ value: "WHITE", label: "White" },
	],
});

const aspectRatioOptions = createListCollection({
	items: [
		{ value: "2x3", label: "2×3" },
		{ value: "4x3", label: "4×3" },
		{ value: "16x9", label: "16×9" },
	],
});

const artworkSizeOptions = createListCollection({
	items: [
		{ value: "Sm", label: "Small" },
		{ value: "Md", label: "Medium" },
		{ value: "Lg", label: "Large" },
	],
});

export const SDSettingsComponent = ({
	settings,
	handleChange: handleSdSettingsChange,
}: {
	settings: components["schemas"]["SDSettings"];
	handleChange: <K extends keyof components["schemas"]["SDSettings"]>(
		key: K,
		value: components["schemas"]["SDSettings"][K],
	) => void;
}) => {
	return (
		<Stack borderWidth="1px" borderRadius="md" p={4} boxShadow="sm" gap={6}>
			<Heading as="h3" size="md" mb={4}>
				SchedulesDirect Settings
			</Heading>

			<Box borderWidth="1px" borderRadius="md" p={4} boxShadow="sm">
				<Heading as="h4" size="sm" mb={4}>
					Account and Data
				</Heading>
				<SimpleGrid columns={{ base: 1, md: 2 }} gap={4}>
					<GridItem>
						<InputField
							label="Username"
							value={settings.sdUserName}
							onChange={(value) => handleSdSettingsChange("sdUserName", value)}
							helperText="Schedules Direct username."
						/>
					</GridItem>
					<GridItem>
						<InputField
							label="Password"
							value={settings.sdPassword}
							onChange={(value) => handleSdSettingsChange("sdPassword", value)}
							helperText="Schedules Direct password."
						/>
					</GridItem>
					<GridItem>
						<InputField
							label="Country"
							value={settings.sdCountry}
							onChange={(value) => handleSdSettingsChange("sdCountry", value)}
							helperText="Schedules Direct country code."
						/>
					</GridItem>
					<GridItem>
						<InputField
							label="Postal/ZIP Code"
							value={settings.sdPostalCode}
							onChange={(value) =>
								handleSdSettingsChange("sdPostalCode", value)
							}
							helperText="Schedules Direct postal code."
						/>
					</GridItem>
					<GridItem>
						<InputField
							label="EPG Days"
							type="number"
							value={settings.sdepgDays}
							onChange={(value) => handleSdSettingsChange("sdepgDays", value)}
							helperText="Number of days for EPG data from Schedules Direct."
						/>
					</GridItem>
				</SimpleGrid>
			</Box>

			<Box borderWidth="1px" borderRadius="md" p={4} boxShadow="sm">
				<Heading as="h4" size="sm" mb={4}>
					Image Settings
				</Heading>
				<SimpleGrid columns={{ base: 1, md: 2 }} gap={4}>
					<GridItem>
						<SelectRoot
							onValueChange={({ value }) =>
								handleSdSettingsChange(
									"artworkSize",
									value[0] as components["schemas"]["SDSettings"]["artworkSize"],
								)
							}
							value={[settings.artworkSize || "Md"]}
							collection={artworkSizeOptions}
						>
							<SelectLabel>Artwork Size</SelectLabel>
							<SelectTrigger>
								<SelectValueText placeholder="Select Artwork Size" />
							</SelectTrigger>
							<SelectContent>
								{artworkSizeOptions.items.map((option) => (
									<SelectItem key={option.value} item={option}>
										{option.label}
									</SelectItem>
								))}
							</SelectContent>
						</SelectRoot>
						<Box fontSize="xs" color="gray.500">
							Size of the artwork images.
						</Box>
					</GridItem>
					<GridItem>
						<SelectRoot
							onValueChange={({ value }) =>
								handleSdSettingsChange(
									"preferredLogoStyle",
									value[0] as components["schemas"]["SDSettings"]["preferredLogoStyle"],
								)
							}
							value={[settings.preferredLogoStyle || "Dark"]}
							collection={logoStyleOptions}
						>
							<SelectLabel>Preferred Logo Style</SelectLabel>
							<SelectTrigger>
								<SelectValueText placeholder="Select Preferred Logo Style" />
							</SelectTrigger>
							<SelectContent>
								{logoStyleOptions.items.map((option) => (
									<SelectItem key={option.value} item={option}>
										{option.label}
									</SelectItem>
								))}
							</SelectContent>
						</SelectRoot>
						<Box fontSize="xs" color="gray.500">
							Preferred logo style for the artwork.
						</Box>
					</GridItem>
					<GridItem>
						<SelectRoot
							onValueChange={({ value }) =>
								handleSdSettingsChange(
									"alternateLogoStyle",
									value[0] as components["schemas"]["SDSettings"]["alternateLogoStyle"],
								)
							}
							value={[settings.alternateLogoStyle || "Light"]}
							collection={logoStyleOptions}
						>
							<SelectLabel>Alternate Logo Style</SelectLabel>
							<SelectTrigger>
								<SelectValueText placeholder="Select Alternate Logo Style" />
							</SelectTrigger>
							<SelectContent>
								{logoStyleOptions.items.map((option) => (
									<SelectItem key={option.value} item={option}>
										{option.label}
									</SelectItem>
								))}
							</SelectContent>
						</SelectRoot>
						<Box fontSize="xs" color="gray.500">
							Alternate logo style for the artwork.
						</Box>
					</GridItem>
					<GridItem>
						<SelectRoot
							onValueChange={({ value }) =>
								handleSdSettingsChange(
									"seriesPosterAspect",
									value[0] as components["schemas"]["SDSettings"]["seriesPosterAspect"],
								)
							}
							value={[settings.seriesPosterAspect || "16:9"]}
							collection={aspectRatioOptions}
						>
							<SelectLabel>Series Poster Aspect</SelectLabel>
							<SelectTrigger>
								<SelectValueText placeholder="Select Series Poster Aspect" />
							</SelectTrigger>
							<SelectContent>
								{aspectRatioOptions.items.map((option) => (
									<SelectItem key={option.value} item={option}>
										{option.label}
									</SelectItem>
								))}
							</SelectContent>
						</SelectRoot>
						<Box fontSize="xs" color="gray.500">
							Aspect ratio for series posters.
						</Box>
					</GridItem>
					<GridItem>
						<InputField
							label="Movie Poster Aspect"
							value={settings.moviePosterAspect}
							onChange={(value) =>
								handleSdSettingsChange("moviePosterAspect", value)
							}
							helperText="Aspect ratio for movie posters."
						/>
					</GridItem>
					<GridItem>
						<SwitchField
							label="Movie Images"
							isChecked={settings.movieImages}
							onChange={(checked) =>
								handleSdSettingsChange("movieImages", checked)
							}
							helperText="Fetch movie images."
						/>
					</GridItem>
					<GridItem>
						<SwitchField
							label="Season Images"
							isChecked={settings.seasonImages}
							onChange={(checked) =>
								handleSdSettingsChange("seasonImages", checked)
							}
							helperText="Fetch season images."
						/>
					</GridItem>
					<GridItem>
						<SwitchField
							label="Series Images"
							isChecked={settings.seriesImages}
							onChange={(checked) =>
								handleSdSettingsChange("seriesImages", checked)
							}
							helperText="Fetch series images."
						/>
					</GridItem>
					<GridItem>
						<SwitchField
							label="Sports Images"
							isChecked={settings.sportsImages}
							onChange={(checked) =>
								handleSdSettingsChange("sportsImages", checked)
							}
							helperText="Fetch sports images."
						/>
					</GridItem>
					<GridItem>
						<SwitchField
							label="Episode Images"
							isChecked={settings.episodeImages}
							onChange={(checked) =>
								handleSdSettingsChange("episodeImages", checked)
							}
							helperText="Fetch episode images."
						/>
					</GridItem>
				</SimpleGrid>
			</Box>

			<Box borderWidth="1px" borderRadius="md" p={4} boxShadow="sm">
				<Heading as="h4" size="sm" mb={4}>
					XMLTV Settings
				</Heading>
				<SimpleGrid columns={{ base: 1, md: 2 }} gap={4}>
					<GridItem>
						<SwitchField
							label="XMLTV Add Filler Data"
							isChecked={settings.xmltvAddFillerData}
							onChange={(checked) =>
								handleSdSettingsChange("xmltvAddFillerData", checked)
							}
							helperText="Add filler data to the XMLTV output."
						/>
					</GridItem>
					<GridItem>
						<InputField
							label="XMLTV Filler Program Length"
							type="number"
							value={settings.xmltvFillerProgramLength}
							onChange={(value) =>
								handleSdSettingsChange("xmltvFillerProgramLength", value)
							}
							helperText="Length of the filler programs in XMLTV."
						/>
					</GridItem>
					<GridItem>
						<SwitchField
							label="XMLTV Extended Info In Title Descriptions"
							isChecked={settings.xmltvExtendedInfoInTitleDescriptions}
							onChange={(checked) =>
								handleSdSettingsChange(
									"xmltvExtendedInfoInTitleDescriptions",
									checked,
								)
							}
							helperText="Include extended information in the XMLTV title descriptions."
						/>
					</GridItem>
					<GridItem>
						<SwitchField
							label="XMLTV Include Channel Numbers"
							isChecked={settings.xmltvIncludeChannelNumbers}
							onChange={(checked) =>
								handleSdSettingsChange("xmltvIncludeChannelNumbers", checked)
							}
							helperText="Include channel numbers in the XMLTV output."
						/>
					</GridItem>
					<GridItem>
						<SwitchField
							label="XMLTV Single Image"
							isChecked={settings.xmltvSingleImage}
							onChange={(checked) =>
								handleSdSettingsChange("xmltvSingleImage", checked)
							}
							helperText="Use a single image for the XMLTV output."
						/>
					</GridItem>
				</SimpleGrid>
			</Box>

			<Box borderWidth="1px" borderRadius="md" p={4} boxShadow="sm">
				<Heading as="h4" size="sm" mb={4}>
					Advanced Settings
				</Heading>
				<SimpleGrid columns={{ base: 1, md: 2 }} gap={4}>
					<GridItem>
						<SwitchField
							label="Alternate SE Format"
							isChecked={settings.alternateSEFormat}
							onChange={(checked) =>
								handleSdSettingsChange("alternateSEFormat", checked)
							}
							helperText="Use alternate SE format."
						/>
					</GridItem>
					<GridItem>
						<SwitchField
							label="Exclude Cast And Crew"
							isChecked={settings.excludeCastAndCrew}
							onChange={(checked) =>
								handleSdSettingsChange("excludeCastAndCrew", checked)
							}
							helperText="Exclude cast and crew from EPG data."
						/>
					</GridItem>
					<GridItem>
						<SwitchField
							label="Prefix Episode Title"
							isChecked={settings.prefixEpisodeTitle}
							onChange={(checked) =>
								handleSdSettingsChange("prefixEpisodeTitle", checked)
							}
							helperText="Prefix episode titles in the EPG."
						/>
					</GridItem>
					<GridItem>
						<SwitchField
							label="Append Episode Desc"
							isChecked={settings.appendEpisodeDesc}
							onChange={(checked) =>
								handleSdSettingsChange("appendEpisodeDesc", checked)
							}
							helperText="Append episode descriptions."
						/>
					</GridItem>
					<GridItem>
						<SwitchField
							label="Prefix Episode Description"
							isChecked={settings.prefixEpisodeDescription}
							onChange={(checked) =>
								handleSdSettingsChange("prefixEpisodeDescription", checked)
							}
							helperText="Prefix episode descriptions."
						/>
					</GridItem>
					<GridItem>
						<SwitchField
							label="Episode Append Program Description"
							isChecked={settings.episodeAppendProgramDescription}
							onChange={(checked) =>
								handleSdSettingsChange(
									"episodeAppendProgramDescription",
									checked,
								)
							}
							helperText="Append program descriptions to episode descriptions."
						/>
					</GridItem>
					<GridItem>
						<SwitchField
							label="SD Enabled"
							isChecked={settings.sdEnabled}
							onChange={(checked) =>
								handleSdSettingsChange("sdEnabled", checked)
							}
							helperText="Enable Schedules Direct."
						/>
					</GridItem>
					<GridItem>
						<InputField
							label="Max Subscribed Lineups"
							type="number"
							value={settings.maxSubscribedLineups}
							onChange={(value) =>
								handleSdSettingsChange("maxSubscribedLineups", value)
							}
							helperText="Maximum number of subscribed lineups."
						/>
					</GridItem>
				</SimpleGrid>
			</Box>
		</Stack>
	);
};
