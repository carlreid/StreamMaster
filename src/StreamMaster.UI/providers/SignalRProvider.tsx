import {
	type HubConnection,
	HubConnectionBuilder,
	LogLevel,
} from "@microsoft/signalr";
import { useEffect, useState, createContext, type ReactNode } from "react";
import type { components } from "../lib/api.d";
import { toaster } from "../components/ui/toaster";
import type { ClearByTag, FieldData } from "../domain/entities";

type M3UFileDto = components["schemas"]["M3UFileDto"];
type EPGFileDto = components["schemas"]["EPGFileDto"];
type QueryStringParameters = components["schemas"]["QueryStringParameters"];
type APIResponse = components["schemas"]["APIResponse"];
type PagedResponseOfM3UFileDto =
	components["schemas"]["PagedResponseOfM3UFileDto"];
type PagedResponseOfEPGFileDto =
	components["schemas"]["PagedResponseOfEPGFileDto"];

type CreateM3UFileRequest = components["schemas"]["CreateM3UFileRequest"];
type UpdateM3UFileRequest = components["schemas"]["UpdateM3UFileRequest"];
type DeleteM3UFileRequest = components["schemas"]["DeleteM3UFileRequest"];
type CreateEPGFileRequest = components["schemas"]["CreateEPGFileRequest"];
type UpdateEPGFileRequest = components["schemas"]["UpdateEPGFileRequest"];
type DeleteEPGFileRequest = components["schemas"]["DeleteEPGFileRequest"];

type ChannelMetric = components["schemas"]["ChannelMetric"];

export interface SignalRMethods {
	GetPagedM3UFiles: (
		params: QueryStringParameters,
	) => Promise<PagedResponseOfM3UFileDto>;
	CreateM3UFile: (request: CreateM3UFileRequest) => Promise<APIResponse>;
	UpdateM3UFile: (request: UpdateM3UFileRequest) => Promise<APIResponse>;
	DeleteM3UFile: (request: DeleteM3UFileRequest) => Promise<APIResponse>;
	GetPagedEPGFiles: (
		params: QueryStringParameters,
	) => Promise<PagedResponseOfEPGFileDto>;
	CreateEPGFile: (request: CreateEPGFileRequest) => Promise<APIResponse>;
	DeleteEPGFile: (request: DeleteEPGFileRequest) => Promise<APIResponse>;
	UpdateEPGFile: (request: UpdateEPGFileRequest) => Promise<APIResponse>;
	GetChannelMetrics: () => Promise<ChannelMetric[]>;
	TaskIsRunning: (taskName: string) => Promise<boolean>;
	CancelAllChannels: () => void;
	CancelChannel: (
		cancelChannelRequest: components["schemas"]["CancelChannelRequest"],
	) => void;
	CancelClient: (
		cancelClientRequest: components["schemas"]["CancelClientRequest"],
	) => void;
	MoveToNextStream: (
		moveToNextStreamRequest: components["schemas"]["MoveToNextStreamRequest"],
	) => void;
}

export interface SignalREvents {
	M3UFileUpdated: (file: M3UFileDto) => void;
	M3UFileCreated: (file: M3UFileDto) => void;
	M3UFileDeleted: (id: number) => void;
	EPGFileUpdated: (file: EPGFileDto) => void;
	EPGFileCreated: (file: EPGFileDto) => void;
	EPGFileDeleted: (id: number) => void;
	SendMessage: (message: components["schemas"]["SMMessage"]) => void;
	DataRefresh: (entity: string) => void;
	SetField: (fieldDatas: FieldData[]) => void;
	ClearByTag: (clearByTag: ClearByTag) => void;
	AuthLogOut: () => void;
	GetChannelMetrics: (channelMetrics: ChannelMetric) => void;
}

export type DataRefreshCallback = (entity: keyof SignalRMethods) => void;
export type MessageCallback = (
	message: components["schemas"]["SMMessage"],
) => void;
export type FieldCallback = (fieldDatas: FieldData[]) => void;
export type ClearByTagCallback = (tagData: ClearByTag) => void;
export type AuthLogoutCallback = () => void;

// Custom event types for internal communication
type InternalEvent = "signalr_connected" | "signalr_disconnected";
type EventListener = (...args: unknown[]) => void;

class SignalRService {
	private connection: HubConnection | null = null;
	private isConnected = false;
	private eventHandlers: Partial<
		Record<keyof SignalREvents, (...args: unknown[]) => void>
	> = {};
	private static instance: SignalRService;

	// Callback storage
	private dataRefreshCallbacks: Set<DataRefreshCallback> = new Set();
	private messageCallbacks: Set<MessageCallback> = new Set();
	private fieldCallbacks: Set<FieldCallback> = new Set();
	private clearByTagCallbacks: Set<ClearByTagCallback> = new Set();
	private authLogoutCallbacks: Set<AuthLogoutCallback> = new Set();
	private eventListeners: Record<string, Set<EventListener>> = {};

	private constructor() {
		this.buildConnection();
	}

	public static getInstance(): SignalRService {
		if (!SignalRService.instance) {
			SignalRService.instance = new SignalRService();
		}
		return SignalRService.instance;
	}

	private buildConnection = (): void => {
		const baseUrl = process.env.NEXT_PUBLIC_BACKEND_API_BASE_URL;
		if (!baseUrl) {
			console.error(
				"Missing NEXT_PUBLIC_BACKEND_API_BASE_URL environment variable",
			);
			return;
		}

		this.connection = new HubConnectionBuilder()
			.withUrl(`${baseUrl}/streammasterhub`)
			.withAutomaticReconnect({
				nextRetryDelayInMilliseconds: (retryContext) => {
					return retryContext.elapsedMilliseconds < 60000 ? 2000 : 2000;
				},
			})
			.configureLogging(LogLevel.Information)
			.build();

		this.connection.onclose(() => {
			this.isConnected = false;
			this.triggerEvent("signalr_disconnected");
		});

		this.startConnection();
	};

	private startConnection = async (): Promise<void> => {
		if (!this.connection) return;

		try {
			await this.connection.start();
			this.isConnected = true;
			console.log("SignalR Connected");
			this.triggerEvent("signalr_connected");
			this.registerEventHandlers();
		} catch (err) {
			console.error("SignalR Connection Error:", err);
			setTimeout(this.startConnection, 5000);
		}
	};

	private registerEventHandlers = (): void => {
		if (!this.connection) return;

		this.connection.on(
			"SendMessage",
			(message: components["schemas"]["SMMessage"]) => {
				console.log(message);
				toaster.create({
					title:
						// Don't show a basic title if it matches severity
						message.summary?.toLowerCase() !== message.severity
							? message.summary
							: undefined,
					description: message.detail,
					type: message.severity,
				});
			},
		);

		this.connection.on("DataRefresh", (entity: keyof SignalRMethods) => {
			for (const dataRefreshCallback of this.dataRefreshCallbacks) {
				dataRefreshCallback(entity);
			}
		});

		this.connection.on("SetField", (fieldDatas: FieldData[]) => {
			for (const fieldCallback of this.fieldCallbacks) {
				fieldCallback(fieldDatas);
			}
		});

		this.connection.on("ClearByTag", (clearByTag: ClearByTag) => {
			for (const clearByTagCallback of this.clearByTagCallbacks) {
				clearByTagCallback(clearByTag);
			}
		});

		this.connection.on("AuthLogOut", () => {
			for (const authLogoutCallback of this.authLogoutCallbacks) {
				authLogoutCallback();
			}
		});

		for (const [event, handler] of Object.entries(this.eventHandlers)) {
			if (
				event !== "SendMessage" &&
				event !== "DataRefresh" &&
				event !== "SetField" &&
				event !== "ClearByTag" &&
				event !== "AuthLogOut"
			) {
				this.connection.on(event, handler);
			}
		}

		this.connection.on("taskisrunning", (taskData) => {
			console.log("SignalR taskisrunning event received:", taskData);
		});
	};

	public invoke = async <T extends keyof SignalRMethods>(
		method: T,
		...args: Parameters<SignalRMethods[T]>
	): Promise<ReturnType<SignalRMethods[T]>> => {
		if (!this.connection || !this.isConnected) {
			throw new Error("SignalR not connected");
		}

		try {
			const result = await this.connection.invoke(method, ...args);

			if (
				result &&
				typeof result === "object" &&
				"isError" in result &&
				result.isError
			) {
				throw new Error(
					(result as { errorMessage?: string; message?: string })
						.errorMessage ||
						(result as { errorMessage?: string; message?: string }).message ||
						"Unknown SignalR error",
				);
			}

			return result as ReturnType<SignalRMethods[T]>;
		} catch (error) {
			console.error(`Error invoking ${String(method)}:`, error);
			throw error;
		}
	};

	// Generic event registration methods
	public addDataRefreshListener = (callback: DataRefreshCallback): void => {
		this.dataRefreshCallbacks.add(callback);
	};

	public removeDataRefreshListener = (callback: DataRefreshCallback): void => {
		this.dataRefreshCallbacks.delete(callback);
	};

	public addMessageListener = (callback: MessageCallback): void => {
		this.messageCallbacks.add(callback);
	};

	public removeMessageListener = (callback: MessageCallback): void => {
		this.messageCallbacks.delete(callback);
	};

	public addFieldListener = (callback: FieldCallback): void => {
		this.fieldCallbacks.add(callback);
	};

	public removeFieldListener = (callback: FieldCallback): void => {
		this.fieldCallbacks.delete(callback);
	};

	public addClearByTagListener = (callback: ClearByTagCallback): void => {
		this.clearByTagCallbacks.add(callback);
	};

	public removeClearByTagListener = (callback: ClearByTagCallback): void => {
		this.clearByTagCallbacks.delete(callback);
	};

	public addAuthLogoutListener = (callback: AuthLogoutCallback): void => {
		this.authLogoutCallbacks.add(callback);
	};

	public removeAuthLogoutListener = (callback: AuthLogoutCallback): void => {
		this.authLogoutCallbacks.delete(callback);
	};

	// Specific Event Handlers
	public addListener<T extends keyof SignalREvents>(
		event: T,
		callback: SignalREvents[T],
	): void {
		if (!this.connection) return;

		this.eventHandlers[event] = callback as (...args: unknown[]) => void;

		if (this.isConnected) {
			this.connection.on(event, callback as (...args: unknown[]) => void);
		}
	}

	public removeListener<T extends keyof SignalREvents>(event: T): void {
		if (!this.connection || !this.eventHandlers[event]) return;

		const handler = this.eventHandlers[event];
		if (handler) {
			this.connection.off(event, handler);
			delete this.eventHandlers[event];
		}
	}

	// Internal event management
	public addEventListener(event: InternalEvent, listener: EventListener): void {
		if (!this.eventListeners[event]) {
			this.eventListeners[event] = new Set();
		}
		this.eventListeners[event].add(listener);
	}

	public removeEventListener(
		event: InternalEvent,
		listener: EventListener,
	): void {
		if (this.eventListeners[event]) {
			this.eventListeners[event].delete(listener);
		}
	}

	private triggerEvent(event: InternalEvent, ...args: unknown[]): void {
		if (this.eventListeners[event]) {
			for (const listener of this.eventListeners[event]) {
				listener(...args);
			}
		}
	}

	public getIsConnected = (): boolean => {
		return this.isConnected;
	};

	public getConnection = (): HubConnection | null => {
		return this.connection;
	};
}

export interface SignalRContextValue {
	service: SignalRService;
	isConnected: boolean;
}

export const SignalRContext = createContext<SignalRContextValue | undefined>(
	undefined,
);

interface SignalRProviderProps {
	children: ReactNode;
}

export const SignalRProvider: React.FC<SignalRProviderProps> = ({
	children,
}) => {
	const signalRService = SignalRService.getInstance();
	const [isConnected, setIsConnected] = useState(
		signalRService.getIsConnected(),
	);

	// biome-ignore lint/correctness/useExhaustiveDependencies: Used as client side init method
	useEffect(() => {
		const handleConnect = () => {
			setIsConnected(true);
		};

		const handleDisconnect = () => {
			setIsConnected(false);
		};

		signalRService.addEventListener("signalr_connected", handleConnect);
		signalRService.addEventListener("signalr_disconnected", handleDisconnect);

		return () => {
			signalRService.removeEventListener("signalr_connected", handleConnect);
			signalRService.removeEventListener(
				"signalr_disconnected",
				handleDisconnect,
			);
		};
	}, []);

	const contextValue = {
		service: signalRService,
		isConnected,
	};

	return (
		<SignalRContext.Provider value={contextValue}>
			{children}
		</SignalRContext.Provider>
	);
};
