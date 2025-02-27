import { Inter } from "next/font/google";
import Provider from "./provider";
import Sidebar from "../components/sidebar";

const inter = Inter({
	subsets: ["latin"],
	display: "swap",
});

export default function RootLayout({
	children,
}: {
	children: React.ReactNode;
}) {
	return (
		<html suppressHydrationWarning className={inter.className} lang="en">
			<head />
			<body>
				<Provider>
					<Sidebar />
					{children}
				</Provider>
			</body>
		</html>
	);
}
