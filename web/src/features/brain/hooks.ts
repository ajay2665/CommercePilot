"use client";

import { useMutation } from "@tanstack/react-query";
import { brainApi } from "@/lib/api";

export function useBrainChat() {
  return useMutation({ mutationFn: brainApi.chat });
}
