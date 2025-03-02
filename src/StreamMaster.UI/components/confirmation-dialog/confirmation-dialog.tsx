import { Button, DialogActionTrigger, Text } from "@chakra-ui/react";
import type { ReactNode } from "react";
import {
	DialogBody,
	DialogCloseTrigger,
	DialogContent,
	DialogFooter,
	DialogHeader,
	DialogRoot,
	DialogTitle,
	DialogTrigger,
} from "../ui/dialog";

type ConfirmationVariant = "warning" | "danger" | "info";

interface ConfirmationDialogProps {
	title: string;
	description: string;
	confirmText?: string;
	cancelText?: string;
	onConfirm: () => void;
	variant?: ConfirmationVariant;
	isLoading?: boolean;
	trigger: ReactNode;
}

export const ConfirmationDialog = ({
	title,
	description,
	confirmText = "Confirm",
	cancelText = "Cancel",
	onConfirm,
	variant = "info",
	isLoading = false,
	trigger,
}: ConfirmationDialogProps) => {
	// Map variants to colors
	const variantColors = {
		warning: "orange",
		danger: "red",
		info: "blue",
	};

	const color = variantColors[variant];

	return (
		<DialogRoot>
			<DialogTrigger asChild>{trigger}</DialogTrigger>
			<DialogContent portalled>
				<DialogHeader>
					<DialogTitle color={`${color}.500`}>{title}</DialogTitle>
				</DialogHeader>
				<DialogBody>
					<Text>{description}</Text>
				</DialogBody>
				<DialogFooter>
					<DialogCloseTrigger />
					<DialogActionTrigger asChild>
						<Button size="sm" variant="outline" loading={isLoading}>
							{cancelText}
						</Button>
					</DialogActionTrigger>
					<DialogActionTrigger asChild>
						<Button
							colorPalette={color}
							size="sm"
							onClick={onConfirm}
							loading={isLoading}
						>
							{confirmText}
						</Button>
					</DialogActionTrigger>
				</DialogFooter>
			</DialogContent>
		</DialogRoot>
	);
};
