import createClient from "openapi-fetch";
import type { paths } from "./api.d";

export const apiClient = createClient<paths>({
	baseUrl: process.env.NEXT_PUBLIC_BACKEND_API_BASE_URL,
});
