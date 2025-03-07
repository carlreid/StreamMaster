import { useCallback, useContext, useEffect, useRef } from "react";
import {
	SignalRContext,
	type SignalRMethods,
	type SignalREvents,
} from "../providers/SignalRProvider";
import type { components } from "./api.d";

export function useSignalR() {
	const context = useContext(SignalRContext);

	if (!context) {
		throw new Error("useSignalR must be used within a SignalRProvider");
	}

	const { service, isConnected } = context;

	// Store entity-method mappings for auto-refresh
	const refreshMappings = useRef<
		Map<
			keyof SignalRMethods,
			{
				args: Parameters<SignalRMethods[keyof SignalRMethods]>;
				callback: (data: Promise<components>) => void;
			}
		>
	>(new Map());

	// Set up data refresh listener once
	useEffect(() => {
		if (!isConnected) return;

		const handleDataRefresh = (entity: keyof SignalRMethods) => {
			const mapping = refreshMappings.current.get(entity);
			if (mapping) {
				// Re-invoke the method with the same args when entity is refreshed
				service
					.invoke(entity, ...mapping.args)
					.then((data) => mapping.callback(data))
					.catch((err) =>
						console.error(`Auto-refresh failed for ${entity}:`, err),
					);
			}
		};

		service.addDataRefreshListener(handleDataRefresh);

		return () => {
			service.removeDataRefreshListener(handleDataRefresh);
		};
	}, [isConnected, service]);

	const invoke = useCallback(
		async <T extends keyof SignalRMethods>(
			method: T,
			...args: Parameters<SignalRMethods[T]>
		): Promise<ReturnType<SignalRMethods[T]>> => {
			return service.invoke(method, ...args);
		},
		[service],
	);

	// Enhanced invoke that sets up auto-refresh
	const invokeWithRefresh = useCallback(
		async <T extends keyof SignalRMethods, TReturn>(
			method: T,
			callback: (data: Promise<TReturn>) => void,
			...args: Parameters<SignalRMethods[T]>
		): Promise<ReturnType<SignalRMethods[T]>> => {
			const result = await service.invoke(method, ...args);

			// Store the mapping for auto-refresh
			refreshMappings.current.set(method, {
				args,
				callback,
			});

			// Return the result as normal
			return result;
		},
		[service],
	);

	const on = useCallback(
		<T extends keyof SignalREvents>(
			event: T,
			callback: SignalREvents[T],
		): void => {
			service.addListener(event, callback);
		},
		[service],
	);

	const off = useCallback(
		<T extends keyof SignalREvents>(event: T): void => {
			service.removeListener(event);
		},
		[service],
	);

	// Method to clear refresh mappings when component unmounts
	const clearRefreshMappings = useCallback((entity?: keyof SignalRMethods) => {
		if (entity) {
			refreshMappings.current.delete(entity);
		} else {
			refreshMappings.current.clear();
		}
	}, []);

	return {
		invoke,
		invokeWithRefresh,
		on,
		off,
		clearRefreshMappings,
		isConnected,
	};
}
