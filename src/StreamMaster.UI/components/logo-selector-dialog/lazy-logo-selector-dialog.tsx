import { Suspense, lazy } from "react";
import type { LogoDialogProps } from "./logo-selector-dialog";

const LazyLogoDialogContent = lazy(() =>
	import("./logo-selector-dialog").then((mod) => {
		return { default: mod.LogoSelectorDialog };
	}),
);

export const LazyLogoSelectorDialog = (props: LogoDialogProps) => {
	return (
		<Suspense fallback={props.trigger}>
			<LazyLogoDialogContent {...props} />
		</Suspense>
	);
};
