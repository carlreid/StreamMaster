"use client";
import { VStack, Button } from "@chakra-ui/react";
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
import { type SourceFormProps, InputField } from "./source-management";

const generateRandomColor = (): string => {
	return `#${Math.floor(Math.random() * 16777215)
		.toString(16)
		.padStart(6, "0")}`;
};

const getDefaultFormState =
	(): components["schemas"]["CreateEPGFileRequest"] => ({
		name: "",
		fileName: "",
		urlSource: "",
		epgNumber: 0,
		color: generateRandomColor(),
		timeShift: 0,
		hoursToUpdate: 24,
	});

export const EPGFileForm = ({
	isOpen,
	onClose,
	initialData,
	onSubmit,
	isEditing,
}: SourceFormProps<
	| components["schemas"]["CreateEPGFileRequest"]
	| components["schemas"]["EPGFileDto"]
>) => {
	const [formData, setFormData] = useState<
		components["schemas"]["CreateEPGFileRequest"]
	>(initialData || getDefaultFormState());

	useEffect(() => {
		if (initialData) {
			setFormData(initialData);
		} else if (isOpen) {
			// Reset form with new random color when opening a new form
			setFormData(getDefaultFormState());
		}
	}, [initialData, isOpen]);

	const handleChange = <
		K extends keyof components["schemas"]["CreateEPGFileRequest"],
	>(
		key: K,
		value: components["schemas"]["CreateEPGFileRequest"][K],
	) => {
		setFormData((prev) => ({ ...prev, [key]: value }));
	};

	const handleSubmit = async () => {
		await onSubmit(formData);
		// Clear form data after submission
		setFormData(getDefaultFormState());
		onClose();
	};

	return (
		<DialogRoot open={isOpen} onOpenChange={onClose}>
			<DialogContent portalled>
				<DialogHeader>
					<DialogTitle>
						{isEditing ? "Edit EPG File" : "Add EPG File"}
					</DialogTitle>
				</DialogHeader>
				<DialogBody>
					<VStack gap={4} align="stretch">
						<InputField
							label="Name"
							value={formData.name || ""}
							onChange={(value) => handleChange("name", value)}
							helperText="Display name for this EPG source"
						/>
						<InputField
							label="File Name"
							value={formData.fileName || ""}
							onChange={(value) => handleChange("fileName", value)}
							helperText="Name of the EPG file"
						/>
						<InputField
							label="URL Source"
							value={formData.urlSource || ""}
							onChange={(value) => handleChange("urlSource", value)}
							helperText="URL to the EPG file"
						/>
						<InputField
							label="EPG Number"
							value={formData.epgNumber}
							onChange={(value) => handleChange("epgNumber", Number(value))}
							type="number"
							helperText="Unique identifier for this EPG source"
						/>
						<InputField
							label="Color"
							value={formData.color || "#3182CE"}
							onChange={(value) => handleChange("color", value)}
							type="color"
							helperText="Color for this EPG source in the UI"
						/>
						<InputField
							label="Time Shift"
							// biome-ignore lint/style/noNonNullAssertion: <explanation>
							value={formData.timeShift!}
							onChange={(value) => handleChange("timeShift", Number(value))}
							type="number"
							helperText="Time shift in hours (can be negative)"
						/>
						<InputField
							label="Hours to Update"
							// biome-ignore lint/style/noNonNullAssertion: <explanation>
							value={formData.hoursToUpdate!}
							onChange={(value) => handleChange("hoursToUpdate", Number(value))}
							type="number"
							helperText="How often to update this source (in hours)"
						/>
					</VStack>
				</DialogBody>
				<DialogFooter>
					<DialogCloseTrigger />
					<DialogActionTrigger asChild>
						<Button variant="outline">Cancel</Button>
					</DialogActionTrigger>
					<DialogActionTrigger asChild>
						<Button onClick={handleSubmit}>
							{isEditing ? "Update" : "Add"}
						</Button>
					</DialogActionTrigger>
				</DialogFooter>
			</DialogContent>
		</DialogRoot>
	);
};
