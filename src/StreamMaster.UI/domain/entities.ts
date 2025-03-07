export interface ClearByTag {
	Entity: string;
	Tag: string;
}

export interface FieldData {
	Entity: string;
	Field: string;
	Id: string;
	// biome-ignore lint/suspicious/noExplicitAny: Do not care for response type
	Value: any;
}
