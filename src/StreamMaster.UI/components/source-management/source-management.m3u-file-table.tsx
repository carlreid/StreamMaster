"use client";
import { Stack, HStack, Button, Flex, Table } from "@chakra-ui/react";
import type { components } from "../../lib/api.d";
import { GenericTablePagination } from "../generic-table/generic-table-pagination";
import { GenericTablePageSize } from "../generic-table/generic-table-pagination-size";
import type { SourceTableProps } from "./source-management";

export const M3UFileTable = ({
	data,
	totalItemCount,
	currentPage,
	pageSize,
	onPageChange,
	onEdit,
	onDelete,
	selection,
	setSelection,
}: SourceTableProps<components["schemas"]["M3UFileDto"]>) => {
	const toggleSelection = (id: number) => {
		if (selection.includes(id)) {
			setSelection(selection.filter((item) => item !== id));
		} else {
			setSelection([...selection, id]);
		}
	};

	const toggleAllSelection = () => {
		if (selection.length === data.length) {
			setSelection([]);
		} else {
			setSelection(data.map((item) => item.id));
		}
	};

	return (
		<Stack>
			<Table.Root showColumnBorder size="sm">
				<Table.Header>
					<Table.Row>
						<Table.ColumnHeader width="40px">
							<input
								type="checkbox"
								checked={data.length > 0 && selection.length === data.length}
								onChange={toggleAllSelection}
							/>
						</Table.ColumnHeader>
						<Table.ColumnHeader>Name</Table.ColumnHeader>
						<Table.ColumnHeader>URL</Table.ColumnHeader>
						<Table.ColumnHeader>Stream Count</Table.ColumnHeader>
						<Table.ColumnHeader>Last Updated</Table.ColumnHeader>
						<Table.ColumnHeader width="120px">Actions</Table.ColumnHeader>
					</Table.Row>
				</Table.Header>
				<Table.Body>
					{data.map((item) => (
						<Table.Row key={item.id}>
							<Table.Cell>
								<input
									type="checkbox"
									checked={selection.includes(item.id)}
									onChange={() => toggleSelection(item.id)}
								/>
							</Table.Cell>
							<Table.Cell>{item.name}</Table.Cell>
							<Table.Cell>{item.url}</Table.Cell>
							<Table.Cell>{item.streamCount}</Table.Cell>
							<Table.Cell>
								{item.lastDownloaded
									? new Date(item.lastDownloaded).toLocaleString()
									: "Never"}
							</Table.Cell>
							<Table.Cell>
								<HStack gap={2}>
									<Button size="xs" onClick={() => onEdit(item)}>
										Edit
									</Button>
									<Button
										size="xs"
										colorPalette="red"
										onClick={() => onDelete(item)}
									>
										Delete
									</Button>
								</HStack>
							</Table.Cell>
						</Table.Row>
					))}
				</Table.Body>
			</Table.Root>
			<Flex justify="space-between" align="center" wrap="wrap" gap={2}>
				<GenericTablePageSize
					currentPage={currentPage}
					pageSize={pageSize}
					onPageSizeChange={(newSize) => {
						const firstItemIndex = (currentPage - 1) * pageSize;
						const newPage = Math.floor(firstItemIndex / newSize) + 1;
						onPageChange({ page: newPage, pageSize: newSize });
					}}
				/>
				<GenericTablePagination
					totalItemCount={totalItemCount}
					pageSize={pageSize}
					currentPage={currentPage}
					onPageChange={onPageChange}
				/>
			</Flex>
		</Stack>
	);
};
