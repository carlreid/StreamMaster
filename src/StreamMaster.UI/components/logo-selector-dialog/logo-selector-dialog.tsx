"use client";

import {
	Box,
	Button,
	DialogActionTrigger,
	Flex,
	IconButton,
	Image,
	Input,
	Separator,
	Spinner,
	Text,
} from "@chakra-ui/react";
import { Tabs } from "@chakra-ui/react";
import { useMemo, useRef, useState } from "react";
import { LuX } from "react-icons/lu";
import { Virtuoso } from "react-virtuoso";
import { useApi } from "../../lib/use-api";
import { useColorModeValue } from "../ui/color-mode";
import {
	DialogBody,
	DialogContent,
	DialogFooter,
	DialogHeader,
	DialogRoot,
	DialogTitle,
} from "../ui/dialog";
import { InputGroup } from "../ui/input-group";
import { useLogoDialog } from "./logo-selector-dialog-context";

export interface NormalizedLogo {
	type: "custom" | "standard";
	name: string;
	url: string;
	isReadOnly: boolean;
	id?: number;
}

export interface LogoDialogProps {
	title?: string;
	currentLogoUrl?: string;
	currentLogoDisplayName?: string;
	onClose?: () => void;
}

export const LogoSelectorDialog = ({
	title = "Select Logo",
	currentLogoUrl,
	currentLogoDisplayName,
	onClose,
}: LogoDialogProps) => {
	const { dialogState, closeLogoDialog } = useLogoDialog();

	const { data: standardLogos = [], error: standardError } = useApi(
		"/api/logos/getlogos",
	);
	const { data: customLogos = [], error: customError } = useApi(
		"/api/logos/getcustomlogos",
	);
	const [searchQuery, setSearchQuery] = useState("");
	const [selectedLogo, setSelectedLogo] = useState<NormalizedLogo | null>(null);
	const virtuosoRefs = {
		all: useRef(null),
		custom: useRef(null),
		standard: useRef(null),
	};

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
			onClose?.();
		}
	};

	// Reset selection when dialog opens
	const handleDialogOpen = () => {
		console.log("handleDialogOpen");
		setSelectedLogo(null);
	};

	// Reset state when dialog closes
	const handleDialogClose = () => {
		closeLogoDialog();
		onClose?.();
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

	// Create rows of 4 logos each for virtualized rendering
	const createLogoRows = (logos: NormalizedLogo[]) => {
		const rows = [];
		for (let i = 0; i < logos.length; i += 4) {
			rows.push(logos.slice(i, i + 4));
		}
		return rows;
	};

	const renderLogoRow = (index: number, rowLogos: NormalizedLogo[]) => {
		return (
			<Flex key={index} gap={4} width="100%" marginBottom={4}>
				{rowLogos.map((logo, logoIndex) => (
					<Box
						key={`${logo.type}-${index}-${logoIndex}`}
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
						flex="1"
						maxWidth="calc(25% - 12px)"
					>
						<Image
							src={logo.url}
							alt={logo.name}
							maxH="100px"
							mx="auto"
							width="100%"
							objectFit="scale-down"
						/>
						<Text fontSize="sm" mt={2} textAlign="center" title={logo.name}>
							{logo.name}
						</Text>
						<Text fontSize="xs" color="gray.500" textAlign="center">
							{logo.type === "custom" ? "Custom" : "Standard"}
						</Text>
					</Box>
				))}
				{/* Fill empty slots with placeholder boxes to maintain grid structure */}
				{Array(4 - rowLogos.length)
					.fill(0)
					.map((_, i) => (
						<Box
							key={`empty-${index}-${
								// biome-ignore lint/suspicious/noArrayIndexKey: It's ok
								i
							}`}
							flex="1"
							maxWidth="calc(25% - 12px)"
						/>
					))}
			</Flex>
		);
	};

	const renderLogoGrid = (
		logos: NormalizedLogo[],
		tabKey: "all" | "custom" | "standard",
	) => {
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

		const logoRows = createLogoRows(logos);

		return (
			<Box height="100%" width="100%">
				<Virtuoso
					ref={virtuosoRefs[tabKey]}
					style={{ height: "100%", width: "100%" }}
					totalCount={logoRows.length}
					itemContent={(index) =>
						renderLogoRow(index, logoRows[index] as NormalizedLogo[])
					}
					overscan={5}
				/>
			</Box>
		);
	};

	return (
		<DialogRoot
			open={dialogState.isOpen}
			onOpenChange={(e) => (e.open ? handleDialogOpen() : handleDialogClose())}
			size="cover"
		>
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
							<InputGroup
								w="full"
								endElement={
									<IconButton
										variant="ghost"
										size="sm"
										disabled={!searchQuery}
										onClick={() => setSearchQuery("")}
									>
										<LuX />
									</IconButton>
								}
							>
								<Input
									placeholder="Search logos by name..."
									value={searchQuery}
									onChange={(e) => setSearchQuery(e.target.value)}
								/>
							</InputGroup>
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

							<Box flex="1" overflow="hidden" pr={2}>
								<Tabs.Content value="all" height="100%">
									{renderLogoGrid(allLogos, "all")}
								</Tabs.Content>
								<Tabs.Content value="custom" height="100%">
									{renderLogoGrid(filteredCustomLogos, "custom")}
								</Tabs.Content>
								<Tabs.Content value="standard" height="100%">
									{renderLogoGrid(filteredStandardLogos, "standard")}
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
