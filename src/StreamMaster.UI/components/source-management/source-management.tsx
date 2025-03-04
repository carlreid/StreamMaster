"use client";

import { Box, Button, Flex, Input, Tabs, Text, VStack } from "@chakra-ui/react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";
import { useApi, useMutate } from "../../lib/use-api";
import type { components } from "../../lib/api.d";
import { apiClient } from "../../lib/api";
import { toaster } from "../ui/toaster";
import { Field } from "../ui/field";
import { FieldLabel, FieldHelperText } from "@chakra-ui/react";
import { Switch } from "../ui/switch";
import { M3UFileForm } from "./source-management.m3u-file-form";
import { M3UFileTable } from "./source-management.m3u-file-table";
import { EPGFileForm } from "./source-management.epg-file-form";
import { EPGFileTable } from "./source-management.epg-file-table";

export interface SourceTableProps<T> {
	data: T[];
	totalItemCount: number;
	currentPage: number;
	pageSize: number;
	onPageChange: (pageInfo: { page: number; pageSize: number }) => void;
	onEdit: (item: T) => void;
	onDelete: (item: T) => void;
	selection: number[];
	setSelection: (selection: number[]) => void;
}

export interface SourceFormProps<T> {
	isOpen: boolean;
	onClose: () => void;
	initialData?: T;
	onSubmit: (data: T) => Promise<void>;
	isEditing: boolean;
}

// Reusable form field components
export const SwitchField = ({
	label,
	isChecked,
	onChange,
	helperText,
}: {
	label: string;
	isChecked: boolean | undefined;
	onChange: (checked: boolean) => void;
	helperText?: string;
}) => {
	return (
		<Field>
			<FieldLabel htmlFor={`switch-${label}`}>{label}</FieldLabel>
			<Switch
				id={`switch-${label}`}
				checked={isChecked}
				onCheckedChange={(e) => onChange(e.checked)}
			/>
			{helperText && <FieldHelperText>{helperText}</FieldHelperText>}
		</Field>
	);
};

export const InputField = <TValue extends string | number | undefined>({
	label,
	value,
	onChange,
	type = "text",
	helperText,
}: {
	label: string;
	value: TValue;
	onChange: (value: TValue) => void;
	type?: string;
	helperText?: string;
}) => {
	return (
		<Field>
			<FieldLabel htmlFor={`input-${label}`}>{label}</FieldLabel>
			<Input
				id={`input-${label}`}
				type={type}
				value={value}
				onChange={(e) => onChange(e.target.value as TValue)}
			/>
			{helperText && <FieldHelperText>{helperText}</FieldHelperText>}
		</Field>
	);
};

export const SourceManagement = () => {
	const router = useRouter();
	const pathname = usePathname();
	const searchParams = useSearchParams();

	// State for tabs
	const [activeTab, setActiveTab] = useState("m3u");

	// Pagination state
	const m3uPage = Number.parseInt(searchParams.get("m3uPage") || "1", 10);
	const m3uPageSize = Number.parseInt(
		searchParams.get("m3uPageSize") || "10",
		10,
	);
	const epgPage = Number.parseInt(searchParams.get("epgPage") || "1", 10);
	const epgPageSize = Number.parseInt(
		searchParams.get("epgPageSize") || "10",
		10,
	);

	// Selection state
	const [m3uSelection, setM3uSelection] = useState<number[]>([]);
	const [epgSelection, setEpgSelection] = useState<number[]>([]);

	// Modal state
	const [m3uModalOpen, setM3uModalOpen] = useState(false);
	const [epgModalOpen, setEpgModalOpen] = useState(false);
	const [editingM3u, setEditingM3u] = useState<
		components["schemas"]["M3UFileDto"] | undefined
	>(undefined);
	const [editingEpg, setEditingEpg] = useState<
		components["schemas"]["EPGFileDto"] | undefined
	>(undefined);

	// Fetch data
	const { data: m3uData } = useApi("/api/m3ufiles/getpagedm3ufiles", {
		params: {
			query: {
				PageNumber: m3uPage,
				PageSize: m3uPageSize,
			},
		},
	});

	const { data: epgData } = useApi("/api/epgfiles/getpagedepgfiles", {
		params: {
			query: {
				PageNumber: epgPage,
				PageSize: epgPageSize,
			},
		},
	});

	const mutate = useMutate();

	// biome-ignore lint/correctness/useExhaustiveDependencies: Want to refresh on changes
	useEffect(() => {
		setM3uSelection([]);
	}, [m3uPage, m3uPageSize]);

	// biome-ignore lint/correctness/useExhaustiveDependencies: Want to refresh on changes
	useEffect(() => {
		setEpgSelection([]);
	}, [epgPage, epgPageSize]);

	const handleM3uPageChange = (pageInfo: {
		page: number;
		pageSize: number;
	}) => {
		const params = new URLSearchParams(searchParams);
		params.set("m3uPage", pageInfo.page.toString());
		if (pageInfo.pageSize !== m3uPageSize) {
			params.set("m3uPageSize", pageInfo.pageSize.toString());
		}
		router.push(`${pathname}?${params.toString()}`);
	};

	const handleEpgPageChange = (pageInfo: {
		page: number;
		pageSize: number;
	}) => {
		const params = new URLSearchParams(searchParams);
		params.set("epgPage", pageInfo.page.toString());
		if (pageInfo.pageSize !== epgPageSize) {
			params.set("epgPageSize", pageInfo.pageSize.toString());
		}
		router.push(`${pathname}?${params.toString()}`);
	};

	const handleAddM3u = async (data: components["schemas"]["M3UFileDto"]) => {
		try {
			await apiClient.POST("/api/m3ufiles/createm3ufile", {
				body: data,
			});
			await mutate(["/api/m3ufiles/getpagedm3ufiles"]);
			toaster.create({
				title: "Success",
				description: "M3U file added successfully",
				type: "success",
			});
		} catch (error) {
			toaster.create({
				title: "Error",
				description: "Failed to add M3U file",
				type: "error",
			});
		}
	};

	const handleUpdateM3u = async (data: components["schemas"]["M3UFileDto"]) => {
		try {
			await apiClient.PATCH("/api/m3ufiles/updatem3ufile", {
				body: data,
			});
			await mutate(["/api/m3ufiles/getpagedm3ufiles"]);
			toaster.create({
				title: "Success",
				description: "M3U file updated successfully",
				type: "success",
			});
		} catch (error) {
			toaster.create({
				title: "Error",
				description: "Failed to update M3U file",
				type: "error",
			});
		}
	};

	const handleDeleteM3u = async (item: components["schemas"]["M3UFileDto"]) => {
		try {
			await apiClient.DELETE("/api/m3ufiles/deletem3ufile", {
				body: {
					deleteFile: true,
					id: item.id,
				},
			});
			await mutate(["/api/m3ufiles/getpagedm3ufiles"]);
			toaster.create({
				title: "Success",
				description: "M3U file deleted successfully",
				type: "success",
			});
		} catch (error) {
			toaster.create({
				title: "Error",
				description: "Failed to delete M3U file",
				type: "error",
			});
		}
	};

	const handleAddEpg = async (data: components["schemas"]["EPGFileDto"]) => {
		try {
			await apiClient.POST("/api/epgfiles/createepgfile", {
				body: data,
			});
			await mutate(["/api/epgfiles/getpagedepgfiles"]);
			toaster.create({
				title: "Success",
				description: "EPG file added successfully",
				type: "success",
			});
		} catch (error) {
			toaster.create({
				title: "Error",
				description: "Failed to add EPG file",
				type: "error",
			});
		}
	};

	const handleUpdateEpg = async (data: components["schemas"]["EPGFileDto"]) => {
		try {
			await apiClient.PATCH("/api/epgfiles/updateepgfile", {
				body: data,
			});
			await mutate(["/api/epgfiles/getpagedepgfiles"]);
			toaster.create({
				title: "Success",
				description: "EPG file updated successfully",
				type: "success",
			});
		} catch (error) {
			toaster.create({
				title: "Error",
				description: "Failed to update EPG file",
				type: "error",
			});
		}
	};

	const handleDeleteEpg = async (item: components["schemas"]["EPGFileDto"]) => {
		try {
			await apiClient.DELETE("/api/epgfiles/deleteepgfile", {
				body: {
					deleteFile: true,
					id: item.id,
				},
			});
			await mutate(["/api/epgfiles/getpagedepgfiles"]);
			toaster.create({
				title: "Success",
				description: "EPG file deleted successfully",
				type: "success",
			});
		} catch (error) {
			toaster.create({
				title: "Error",
				description: "Failed to delete EPG file",
				type: "error",
			});
		}
	};

	const handleEditM3u = (item: components["schemas"]["M3UFileDto"]) => {
		setEditingM3u(item);
		setM3uModalOpen(true);
	};

	const handleEditEpg = (item: components["schemas"]["EPGFileDto"]) => {
		setEditingEpg(item);
		setEpgModalOpen(true);
	};

	const handleAddM3uClick = () => {
		setEditingM3u(undefined);
		setM3uModalOpen(true);
	};

	const handleAddEpgClick = () => {
		setEditingEpg(undefined);
		setEpgModalOpen(true);
	};

	const handleM3uSubmit = async (data: components["schemas"]["M3UFileDto"]) => {
		if (editingM3u) {
			await handleUpdateM3u(data);
		} else {
			await handleAddM3u(data);
		}
	};

	const handleEpgSubmit = async (data: components["schemas"]["EPGFileDto"]) => {
		if (editingEpg) {
			await handleUpdateEpg(data);
		} else {
			await handleAddEpg(data);
		}
	};

	return (
		<VStack align="stretch" gap={4}>
			<Tabs.Root
				value={activeTab}
				onValueChange={(e) => setActiveTab(e.value)}
				display="flex"
				flexDirection="column"
				flex="1"
				overflow="hidden"
			>
				<Box flexShrink={0}>
					<Tabs.List>
						<Tabs.Trigger value="m3u">M3U Sources</Tabs.Trigger>
						<Tabs.Trigger value="epg">EPG Sources</Tabs.Trigger>
						<Tabs.Indicator />
					</Tabs.List>
				</Box>

				<Box flex="1" overflow="hidden" pr={2} pt={4}>
					<Tabs.Content value="m3u" height="100%">
						<VStack align="stretch" gap={4}>
							<Flex justify="space-between">
								<Button colorPalette="blue" onClick={handleAddM3uClick}>
									Add M3U Source
								</Button>
							</Flex>
							{m3uData ? (
								<M3UFileTable
									data={m3uData.data || []}
									totalItemCount={m3uData.totalItemCount || 0}
									currentPage={m3uPage}
									pageSize={m3uPageSize}
									onPageChange={handleM3uPageChange}
									onEdit={handleEditM3u}
									onDelete={handleDeleteM3u}
									selection={m3uSelection}
									setSelection={setM3uSelection}
								/>
							) : (
								<Box>Loading M3U sources...</Box>
							)}
						</VStack>
					</Tabs.Content>

					<Tabs.Content value="epg" height="100%">
						<VStack align="stretch" gap={4}>
							<Flex justify="space-between">
								<Text fontSize="lg" fontWeight="bold">
									EPG Sources
								</Text>
								<Button colorPalette="blue" onClick={handleAddEpgClick}>
									Add EPG Source
								</Button>
							</Flex>
							{epgData ? (
								<EPGFileTable
									data={epgData.data || []}
									totalItemCount={epgData.totalItemCount || 0}
									currentPage={epgPage}
									pageSize={epgPageSize}
									onPageChange={handleEpgPageChange}
									onEdit={handleEditEpg}
									onDelete={handleDeleteEpg}
									selection={epgSelection}
									setSelection={setEpgSelection}
								/>
							) : (
								<Box>Loading EPG sources...</Box>
							)}
						</VStack>
					</Tabs.Content>
				</Box>
			</Tabs.Root>

			<M3UFileForm
				isOpen={m3uModalOpen}
				onClose={() => setM3uModalOpen(false)}
				initialData={editingM3u}
				onSubmit={handleM3uSubmit}
				isEditing={!!editingM3u}
			/>

			<EPGFileForm
				isOpen={epgModalOpen}
				onClose={() => setEpgModalOpen(false)}
				initialData={editingEpg}
				onSubmit={handleEpgSubmit}
				isEditing={!!editingEpg}
			/>
		</VStack>
	);
};
