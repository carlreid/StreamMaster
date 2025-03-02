import {
	createContext,
	useContext,
	useState,
	type ReactNode,
	useCallback,
} from "react";
import type { NormalizedLogo } from "./logo-selector-dialog";
import { apiClient } from "../../lib/api";
import { useMutate } from "../../lib/use-api";
import { toaster } from "../ui/toaster";
import type { components } from "../../lib/api.d";

interface LogoDialogContextType {
	closeLogoDialog: () => void;
	dialogState: {
		isOpen: boolean;
		channelId?: number;
		currentLogo?: string;
		channelName?: string;
		onSelect?: (
			logo: NormalizedLogo,
		) => Promise<{ success: boolean; message: string }>;
	};
	openLogoDialog: (
		channel: components["schemas"]["SMChannelDto"],
		onSelect?: (
			logo: NormalizedLogo,
		) => Promise<{ success: boolean; message: string }>,
	) => void;
}

const LogoDialogContext = createContext<LogoDialogContextType | undefined>(
	undefined,
);

export const useLogoDialog = () => {
	const context = useContext(LogoDialogContext);
	if (!context) {
		throw new Error("useLogoDialog must be used within a LogoDialogProvider");
	}
	return context;
};

interface LogoDialogProviderProps {
	children: ReactNode;
}

export const LogoDialogProvider = ({ children }: LogoDialogProviderProps) => {
	const mutate = useMutate();

	const [dialogState, setDialogState] = useState<{
		isOpen: boolean;
		channelId?: number;
		currentLogo?: string;
		channelName?: string;
		onSelect?: (
			logo: NormalizedLogo,
		) => Promise<{ success: boolean; message: string }>;
	}>({
		isOpen: false,
	});

	const handleLogoSelect = useCallback(
		async (
			channelId: number | undefined,
			logo: string,
		): Promise<{ success: boolean; message: string }> => {
			if (!channelId)
				return { success: false, message: "A channel ID is required" };
			try {
				await apiClient.PATCH("/api/smchannels/setsmchannellogo", {
					body: {
						smChannelId: channelId,
						logo: logo,
					},
				});
				await mutate(["/api/smchannels/getpagedsmchannels"]);

				toaster.create({
					title: "Logo updated",
					description: "Logo has been updated successfully",
					type: "success",
				});
				return { success: true, message: "Logo updated" };
			} catch (e) {
				console.error(e);
				return { success: false, message: "Error updating logo" };
			}
		},
		[mutate],
	);

	const openLogoDialog = useCallback(
		(
			channel: components["schemas"]["SMChannelDto"],
			onSelect?: (
				logo: NormalizedLogo,
			) => Promise<{ success: boolean; message: string }>,
		) => {
			setDialogState({
				isOpen: true,
				channelId: channel.id,
				currentLogo: channel.logo,
				channelName: channel.name,
				onSelect:
					onSelect ||
					((logo: NormalizedLogo) => handleLogoSelect(channel.id, logo.url)),
			});
		},
		[handleLogoSelect],
	);

	const closeLogoDialog = useCallback(() => {
		setDialogState((prev) => ({ ...prev, isOpen: false }));
	}, []);

	const value = {
		openLogoDialog,
		dialogState,
		closeLogoDialog,
		handleLogoSelect,
	};

	return (
		<LogoDialogContext.Provider value={value}>
			{children}
		</LogoDialogContext.Provider>
	);
};
