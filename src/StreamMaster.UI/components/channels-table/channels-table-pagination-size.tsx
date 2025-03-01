import { useState, useEffect } from "react";
import { HStack, IconButton, Button, Stack } from "@chakra-ui/react";
import { LuSettings } from "react-icons/lu";
import {
	SelectContent,
	SelectItem,
	SelectLabel,
	SelectRoot,
	SelectTrigger,
	SelectValueText,
} from "../ui/select";
import {
	PopoverBody,
	PopoverContent,
	PopoverRoot,
	PopoverTitle,
	PopoverTrigger,
} from "../ui/popover";
import { NumberInputField, NumberInputRoot } from "../ui/number-input";
import { createListCollection } from "@chakra-ui/react";

// Predefined page size options
const PAGE_SIZE_OPTIONS = [10, 25, 50, 100, 250];

interface PageSizeOption {
	value: string;
	label: string;
	disabled?: boolean;
}

interface ChannelTablePageSizeProps {
	pageSize: number;
	currentPage: number;
	onPageSizeChange: (newSize: number) => void;
}

export const ChannelTablePageSize = ({
	pageSize,
	onPageSizeChange,
}: ChannelTablePageSizeProps) => {
	const [customPageSize, setCustomPageSize] = useState<number>(0);
	const [selectedPageSize, setSelectedPageSize] = useState<string[]>([]);

	// Create the initial page size options
	const pageSizeItems: PageSizeOption[] = PAGE_SIZE_OPTIONS.map((size) => ({
		value: String(size),
		label: String(size),
	}));

	// Add current page size if it's not in the predefined options
	if (pageSize && !PAGE_SIZE_OPTIONS.includes(pageSize)) {
		pageSizeItems.push({
			value: String(pageSize),
			label: String(pageSize),
		});
	}

	// Add custom option
	pageSizeItems.push({
		value: "custom",
		label: "Custom...",
	});

	// Create the collection
	const pageSizeOptions = createListCollection({
		items: pageSizeItems,
	});

	// Initialize selected page size and custom page size when component mounts
	useEffect(() => {
		if (pageSize) {
			const pageSizeStr = String(pageSize);
			setSelectedPageSize([pageSizeStr]);

			if (!PAGE_SIZE_OPTIONS.includes(pageSize)) {
				setCustomPageSize(pageSize);
			}
		}
	}, [pageSize]);

	const handleCustomPageSizeChange = (value: string) => {
		setCustomPageSize(Number(value));
	};

	const applyCustomPageSize = () => {
		if (customPageSize >= 1) {
			onPageSizeChange(customPageSize);
		}
	};

	const handleSelectChange = (value: string[]) => {
		if (value.includes("custom")) {
			// Custom option selected, don't change page size yet
			setSelectedPageSize(["custom"]);
			return;
		}

		if (value.length > 0) {
			const newSize = Number(value[0]);
			setSelectedPageSize(value);
			onPageSizeChange(newSize);
		}
	};

	return (
		<HStack gap={2}>
			<SelectRoot
				value={selectedPageSize}
				collection={pageSizeOptions}
				onValueChange={(e) => handleSelectChange(e.value)}
			>
				<SelectTrigger width="100px">
					<SelectValueText />
				</SelectTrigger>
				<SelectContent>
					<SelectLabel>Page Size</SelectLabel>
					{pageSizeItems.map((option) => (
						<SelectItem key={option.value} item={option}>
							{option.label}
						</SelectItem>
					))}
				</SelectContent>
			</SelectRoot>

			<PopoverRoot>
				<PopoverTrigger asChild>
					<IconButton aria-label="Custom page size" size="sm" variant="ghost">
						<LuSettings />
					</IconButton>
				</PopoverTrigger>
				<PopoverContent>
					<PopoverBody>
						<PopoverTitle>Custom Page Size</PopoverTitle>
						<Stack gap={3} py={2}>
							<NumberInputRoot
								value={String(customPageSize)}
								onValueChange={(e) => handleCustomPageSizeChange(e.value)}
							>
								<NumberInputField min={1} max={1000} />
							</NumberInputRoot>
							<Button size="sm" onClick={applyCustomPageSize}>
								Apply
							</Button>
						</Stack>
					</PopoverBody>
				</PopoverContent>
			</PopoverRoot>
		</HStack>
	);
};
