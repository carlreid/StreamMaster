import { useState, useMemo } from "react";
import {
	Button,
	Input,
	SimpleGrid,
	Box,
	Image,
	Text,
	Flex,
	Spinner,
	Separator,
	DialogActionTrigger,
} from "@chakra-ui/react";
import {
	DialogBody,
	DialogContent,
	DialogFooter,
	DialogHeader,
	DialogRoot,
	DialogTitle,
	DialogTrigger,
} from "../ui/dialog";
import type { ReactNode } from "react";
import { Tabs } from "@chakra-ui/react";
import { useApi } from "../../lib/use-api";
import { useColorModeValue } from "../ui/color-mode";

export interface NormalizedLogo {
	type: "custom" | "standard";
	name: string;
	url: string;
	isReadOnly: boolean;
	id?: number;
}

export interface LogoDialogProps {
	title?: string;
	onSelectLogo: (
		logo: NormalizedLogo,
	) => Promise<{ success: boolean; message: string }>;
	trigger: ReactNode;
	currentLogoUrl?: string;
	currentLogoDisplayName?: string;
}

export const LogoSelectorDialog = ({
	title = "Select Logo",
	onSelectLogo,
	trigger,
	currentLogoUrl,
	currentLogoDisplayName,
}: LogoDialogProps) => {
	// Use useApi directly in the component
	const { data: standardLogos = [], error: standardError } = useApi(
		"/api/logos/getlogos",
	);
	const { data: customLogos = [], error: customError } = useApi(
		"/api/logos/getcustomlogos",
	);
	const [searchQuery, setSearchQuery] = useState("");
	const [selectedLogo, setSelectedLogo] = useState<NormalizedLogo | null>(null);
	const [isDialogOpen, setIsDialogOpen] = useState(false);

	// Colors for selected logo highlight
	const selectedBgColor = useColorModeValue("blue.50", "blue.900");
	const selectedBorderColor = useColorModeValue("blue.500", "blue.400");

	const isLoading =
		(!standardLogos || !customLogos) && !standardError && !customError;

	// Normalize and filter logos based on search query
	const filteredStandardLogos: NormalizedLogo[] = useMemo(() => {
		return standardLogos
			.filter(
				(logo) =>
					logo.name?.toLowerCase().includes(searchQuery.toLowerCase()) ||
					!searchQuery,
			)
			.map((logo) => ({
				name: logo.name || "Unnamed Logo",
				url: logo.source || "",
				type: "standard",
				isReadOnly: logo.isReadOnly || true,
				id: logo.fileId,
			}));
	}, [standardLogos, searchQuery]);

	const filteredCustomLogos: NormalizedLogo[] = useMemo(() => {
		return customLogos
			.filter(
				(logo) =>
					logo.name?.toLowerCase().includes(searchQuery.toLowerCase()) ||
					!searchQuery,
			)
			.map((logo) => ({
				name: logo.name || "Unnamed Logo",
				url: logo.value || logo.source || "",
				isReadOnly: logo.isReadOnly || true,
				id: logo.fileId,
				type: "custom",
			}));
	}, [customLogos, searchQuery]);

	// Combined logos for "All" tab
	const allLogos: NormalizedLogo[] = useMemo(() => {
		return [...filteredCustomLogos, ...filteredStandardLogos];
	}, [filteredCustomLogos, filteredStandardLogos]);

	const handleLogoSelect = (logo: NormalizedLogo) => {
		setSelectedLogo(logo);
	};

	const handleConfirmSelection = async () => {
		if (selectedLogo) {
			const result = await onSelectLogo(selectedLogo);
			if (result.success) {
				setIsDialogOpen(false);
			}
		}
	};

	// Reset selection when dialog opens
	const handleDialogOpen = () => {
		console.log("handleDialogOpen");
		setIsDialogOpen(true);
		setSelectedLogo(null);
	};

	// Reset state when dialog closes
	const handleDialogClose = () => {
		setIsDialogOpen(false);
		setSearchQuery("");
	};

	// Check if a logo is selected
	const isLogoSelected = (logo: NormalizedLogo) => {
		return (
			selectedLogo &&
			selectedLogo.url === logo.url &&
			selectedLogo.name === logo.name
		);
	};

	const renderLogoGrid = (logos: NormalizedLogo[]) => {
		if (isLoading) {
			return (
				<Flex justify="center" align="center" height="100%" minHeight="200px">
					<Spinner size="xl" />
				</Flex>
			);
		}

		if (logos.length === 0) {
			return (
				<Flex justify="center" align="center" height="100%" minHeight="200px">
					<Text>No logos found</Text>
				</Flex>
			);
		}

		return (
			<SimpleGrid columns={3} gap={4} mt={4}>
				{logos.map((logo, index) => (
					<Box
						key={`${logo.type}-${index}`}
						borderWidth="1px"
						borderRadius="lg"
						overflow="hidden"
						cursor="pointer"
						onClick={() => handleLogoSelect(logo)}
						_hover={{ borderColor: "blue.500" }}
						bg={isLogoSelected(logo) ? selectedBgColor : "transparent"}
						borderColor={isLogoSelected(logo) ? selectedBorderColor : "inherit"}
						boxShadow={isLogoSelected(logo) ? "md" : "none"}
						p={2}
						transition="all 0.2s"
					>
						<Image src={logo.url} alt={logo.name} maxH="100px" mx="auto" />
						<Text fontSize="sm" mt={2} textAlign="center" title={logo.name}>
							{logo.name}
						</Text>
						<Text fontSize="xs" color="gray.500" textAlign="center">
							{logo.type === "custom" ? "Custom" : "Standard"}
						</Text>
					</Box>
				))}
			</SimpleGrid>
		);
	};

	return (
		<DialogRoot
			open={isDialogOpen}
			onOpenChange={(e) => (e.open ? handleDialogOpen() : handleDialogClose())}
			size="cover"
		>
			<DialogTrigger asChild>{trigger}</DialogTrigger>
			<DialogContent portalled maxWidth="800px" maxHeight="90vh">
				<Flex direction="column" height="100%">
					<DialogHeader>
						<DialogTitle>{title}</DialogTitle>
					</DialogHeader>

					<DialogBody
						flex="1"
						display="flex"
						flexDirection="column"
						overflow="hidden"
					>
						{currentLogoUrl && (
							<Box mb={4} flexShrink={0}>
								<Text fontWeight="medium" mb={2}>
									Current Logo
								</Text>
								<Flex
									alignItems="center"
									p={3}
									borderWidth="1px"
									borderRadius="md"
									bg={useColorModeValue("gray.50", "gray.700")}
								>
									<Image src={currentLogoUrl} maxH="60px" maxW="100px" mr={4} />
									<Box>
										<Text fontWeight="bold">{currentLogoDisplayName}</Text>
									</Box>
								</Flex>
								<Separator my={4} />
							</Box>
						)}

						<Box mb={4} flexShrink={0}>
							<Input
								placeholder="Search logos by name..."
								value={searchQuery}
								onChange={(e) => setSearchQuery(e.target.value)}
							/>
						</Box>

						<Tabs.Root
							defaultValue="all"
							display="flex"
							flexDirection="column"
							flex="1"
							overflow="hidden"
						>
							<Box flexShrink={0}>
								<Tabs.List>
									<Tabs.Trigger value="all">All Logos</Tabs.Trigger>
									<Tabs.Trigger value="custom">Custom Logos</Tabs.Trigger>
									<Tabs.Trigger value="standard">Standard Logos</Tabs.Trigger>
									<Tabs.Indicator />
								</Tabs.List>
							</Box>

							<Box flex="1" overflow="auto" pr={2}>
								<Tabs.Content value="all" height="100%">
									{renderLogoGrid(allLogos)}
								</Tabs.Content>
								<Tabs.Content value="custom" height="100%">
									{renderLogoGrid(filteredCustomLogos)}
								</Tabs.Content>
								<Tabs.Content value="standard" height="100%">
									{renderLogoGrid(filteredStandardLogos)}
								</Tabs.Content>
							</Box>
						</Tabs.Root>
					</DialogBody>

					<DialogFooter flexShrink={0}>
						<DialogActionTrigger asChild>
							<Button variant="outline" onClick={() => handleDialogClose()}>
								Cancel
							</Button>
						</DialogActionTrigger>
						<Button
							size="sm"
							colorPalette="green"
							disabled={!selectedLogo}
							onClick={handleConfirmSelection}
						>
							{selectedLogo ? "Set Logo" : "Select a Logo"}
						</Button>
					</DialogFooter>
				</Flex>
			</DialogContent>
		</DialogRoot>
	);
};
