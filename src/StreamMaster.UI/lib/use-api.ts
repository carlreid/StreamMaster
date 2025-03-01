import { isMatch } from "lodash-es";
import { createMutateHook, createQueryHook } from "swr-openapi";
import { apiClient } from "./api";

const prefix = "sm-api";

export const useApi = createQueryHook(apiClient, prefix);
export const useMutate = createMutateHook(apiClient, prefix, isMatch);
