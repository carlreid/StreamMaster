"use client";
import {
	VStack,
	Heading,
	HStack,
	Button,
	Box,
	GridItem,
	createListCollection,
} from "@chakra-ui/react";
import { useState, useEffect } from "react";
import type { components } from "../../lib/api.d";
import {
	DialogRoot,
	DialogContent,
	DialogHeader,
	DialogTitle,
	DialogBody,
	DialogFooter,
	DialogCloseTrigger,
	DialogActionTrigger,
} from "../ui/dialog";
import {
	type SourceFormProps,
	InputField,
	SwitchField,
} from "./source-management";
import {
	SelectRoot,
	SelectLabel,
	SelectTrigger,
	SelectValueText,
	SelectContent,
	SelectItem,
} from "../ui/select";

// Enum options for M3UName with string values
const m3uFieldOptions = createListCollection<{
	value: string;
	label: string;
	originalValue: components["schemas"]["CreateM3UFileRequest"]["m3UName"];
}>({
	items: [
		{ value: "0", label: "ChannelId", originalValue: 0 },
		{ value: "1", label: "ChannelName", originalValue: 1 },
		{ value: "2", label: "ChannelNumber", originalValue: 2 },
		{ value: "3", label: "Group", originalValue: 3 },
		{ value: "4", label: "Name", originalValue: 4 },
		{ value: "5", label: "TvgID", originalValue: 5 },
		{ value: "6", label: "TvgName", originalValue: 6 },
	],
});

// Enum options for M3UKey with string values
const m3uKeyOptions = createListCollection<{
	value: string;
	label: string;
	originalValue: components["schemas"]["CreateM3UFileRequest"]["m3UKey"];
}>({
	items: [
		{ value: "0", label: "URL", originalValue: 0 },
		{ value: "1", label: "CUID", originalValue: 1 },
		{ value: "2", label: "ChannelId", originalValue: 2 },
		{ value: "3", label: "TvgID", originalValue: 3 },
		{ value: "4", label: "TvgName", originalValue: 4 },
		{ value: "5", label: "TvgName_TvgID", originalValue: 5 },
		{ value: "6", label: "Name", originalValue: 6 },
		{ value: "7", label: "Name_TvgID", originalValue: 7 },
	],
});

// Command profile options
const commandProfileOptions = createListCollection<{
	value: components["schemas"]["CreateM3UFileRequest"]["m3U8OutPutProfile"];
	label: string;
}>({
	items: [
		{ value: "Default", label: "Default" },
		{ value: "Redirect", label: "Redirect" },
		{ value: "SMFTMPEG", label: "SMFTMPEG" },
		{ value: "SMFFMPEGLocal", label: "SMFFMPEGLocal" },
		{ value: "YT", label: "YT" },
	],
});

const defaultM3uData: components["schemas"]["CreateM3UFileRequest"] = {
	name: "",
	maxStreamCount: 1,
	m3U8OutPutProfile: "SMFTMPEG",
	m3UKey: 4, // TvgName (enum value 4)
	m3UName: 4, // Name (enum value 4)
	defaultStreamGroupName: null,
	urlSource: "",
	syncChannels: false,
	hoursToUpdate: 72,
	startingChannelNumber: 0,
	autoSetChannelNumbers: false,
	vodTags: [],
};

export const M3UFileForm = ({
	isOpen,
	onClose,
	initialData,
	onSubmit,
	isEditing,
}: SourceFormProps<
	| components["schemas"]["CreateM3UFileRequest"]
	| components["schemas"]["M3UFileDto"]
>) => {
	const [formData, setFormData] = useState<
		components["schemas"]["CreateM3UFileRequest"]
	>(initialData || defaultM3uData);

	useEffect(() => {
		if (initialData) {
			setFormData(initialData);
		}
	}, [initialData]);

	const handleChange = <
		K extends keyof components["schemas"]["CreateM3UFileRequest"],
	>(
		key: K,
		value: components["schemas"]["CreateM3UFileRequest"][K],
	) => {
		setFormData((prev) => ({ ...prev, [key]: value }));
	};

	const handleSubmit = async () => {
		await onSubmit(formData);
		setFormData(defaultM3uData);
		onClose();
	};

	return (
		<DialogRoot open={isOpen} onOpenChange={onClose} size="lg">
			<DialogContent>
				<DialogHeader>
					<DialogTitle>
						{isEditing ? "Edit M3U File" : "Add M3U File"}
					</DialogTitle>
				</DialogHeader>
				<DialogBody>
					<VStack gap={6} align="stretch">
						<VStack gap={4} align="stretch">
							<Heading size="sm">Basic Information</Heading>
							<InputField
								label="Name"
								value={formData.name}
								onChange={(value) => handleChange("name", value)}
								helperText="Display name for this M3U source"
							/>
							<InputField
								label="URL"
								// biome-ignore lint/style/noNonNullAssertion: Is set
								value={formData.urlSource!}
								onChange={(value) =>
									handleChange(
										"urlSource",
										value as components["schemas"]["CreateM3UFileRequest"]["urlSource"],
									)
								}
								helperText="URL to the M3U file"
							/>
						</VStack>
						<VStack gap={4} align="stretch">
							<Heading size="sm">Update Settings</Heading>
							<InputField
								label="Hours to Update"
								// biome-ignore lint/style/noNonNullAssertion: Is set
								value={formData.hoursToUpdate!}
								onChange={(value) =>
									handleChange(
										"hoursToUpdate",
										Number(
											value,
										) as components["schemas"]["CreateM3UFileRequest"]["hoursToUpdate"],
									)
								}
								type="number"
								helperText="How often to update this source (in hours)"
							/>
						</VStack>
						<VStack gap={4} align="stretch">
							<Heading size="sm">Stream Settings</Heading>
							<GridItem>
								<SelectRoot
									onValueChange={({ value }) =>
										handleChange(
											"m3U8OutPutProfile",
											value[0] as components["schemas"]["CreateM3UFileRequest"]["m3U8OutPutProfile"],
										)
									}
									value={[formData.m3U8OutPutProfile || "SMFTMPEG"]}
									collection={commandProfileOptions}
								>
									<SelectLabel>M3U8 Command Profile</SelectLabel>
									<SelectTrigger>
										<SelectValueText placeholder="Select Command Profile" />
									</SelectTrigger>
									<SelectContent zIndex={"popover"}>
										{commandProfileOptions.items.map((option) => (
											<SelectItem key={option.value} item={option}>
												{option.label}
											</SelectItem>
										))}
									</SelectContent>
								</SelectRoot>
								<Box fontSize="xs" color="gray.500">
									Command profile to use for processing M3U8 streams
								</Box>
							</GridItem>
							<GridItem>
								<SelectRoot
									onValueChange={({ value }) =>
										handleChange(
											"m3UName",
											Number(
												value[0],
											) as components["schemas"]["CreateM3UFileRequest"]["m3UName"],
										)
									}
									value={[String(formData.m3UName || 4)]}
									collection={m3uFieldOptions}
								>
									<SelectLabel>Channel Name Field</SelectLabel>
									<SelectTrigger>
										<SelectValueText placeholder="Select Channel Name Field" />
									</SelectTrigger>
									<SelectContent zIndex={"popover"}>
										{m3uFieldOptions.items.map((option) => (
											<SelectItem key={option.value} item={option}>
												{option.label}
											</SelectItem>
										))}
									</SelectContent>
								</SelectRoot>
								<Box fontSize="xs" color="gray.500">
									Field to use for channel names
								</Box>
							</GridItem>
							<GridItem>
								<SelectRoot
									onValueChange={({ value }) =>
										handleChange(
											"m3UKey",
											Number(
												value[0],
											) as components["schemas"]["CreateM3UFileRequest"]["m3UKey"],
										)
									}
									value={[String(formData.m3UKey || 4)]}
									collection={m3uKeyOptions}
								>
									<SelectLabel>Key to identify each M3U stream</SelectLabel>
									<SelectTrigger>
										<SelectValueText placeholder="Select M3U Key" />
									</SelectTrigger>
									<SelectContent zIndex={"popover"}>
										{m3uKeyOptions.items.map((option) => (
											<SelectItem key={option.value} item={option}>
												{option.label}
											</SelectItem>
										))}
									</SelectContent>
								</SelectRoot>
								<Box fontSize="xs" color="gray.500">
									Key used to identify unique streams
								</Box>
							</GridItem>
							<InputField
								label="Default Stream Group"
								value={formData.defaultStreamGroupName || ""}
								onChange={(value) =>
									handleChange(
										"defaultStreamGroupName",
										value as components["schemas"]["CreateM3UFileRequest"]["defaultStreamGroupName"],
									)
								}
								helperText="Default group for streams from this source"
							/>
							<InputField
								label="Max Stream Count"
								// biome-ignore lint/style/noNonNullAssertion: Is set
								value={formData.maxStreamCount!}
								onChange={(value) =>
									handleChange("maxStreamCount", Number(value))
								}
								type="number"
								helperText="Maximum number of streams to import (0 for unlimited)"
							/>
						</VStack>
						<VStack gap={4} align="stretch">
							<Heading size="sm">Channel Settings</Heading>
							<SwitchField
								label="Sync Channels"
								isChecked={formData.syncChannels || false}
								onChange={(value) => handleChange("syncChannels", value)}
								helperText="Automatically sync channels with this source"
							/>
							{formData.syncChannels && (
								<HStack gap={4} align="flex-start">
									<InputField
										label="Starting Channel Number"
										// biome-ignore lint/style/noNonNullAssertion: Is set
										value={formData.startingChannelNumber!}
										onChange={(value) =>
											handleChange("startingChannelNumber", Number(value))
										}
										type="number"
										helperText="First channel number for this source"
									/>
									<SwitchField
										label="Auto Set Channel Numbers"
										isChecked={formData.autoSetChannelNumbers || false}
										onChange={(value) =>
											handleChange("autoSetChannelNumbers", value)
										}
										helperText="Automatically assign channel numbers"
									/>
								</HStack>
							)}
						</VStack>
					</VStack>
				</DialogBody>
				<DialogFooter>
					<DialogCloseTrigger />
					<DialogActionTrigger asChild>
						<Button variant="outline">Cancel</Button>
					</DialogActionTrigger>
					<Button onClick={handleSubmit}>{isEditing ? "Update" : "Add"}</Button>
				</DialogFooter>
			</DialogContent>
		</DialogRoot>
	);
};
