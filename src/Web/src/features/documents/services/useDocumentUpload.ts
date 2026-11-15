import { useEffect, useRef, useState } from "react";
import { DOCUMENT_POLL_DURATION_MS, DOCUMENT_POLL_INTERVAL_MS } from "./documentPolling";
import {
  useBeginDocumentUploadMutation,
  useGetDocumentQuery,
} from "../../../services/api/generated/api";
import { useAppDispatch, useAppSelector } from "../../../services/hooks";
import {
  selectCurrentOrganizationId,
  lockOrganizationSwitching,
  unlockOrganizationSwitching,
} from "../../../services/organization/organizationSlice";

interface UseDocumentUploadOptions {
  onCompleted: () => void;
}

export type DocumentUploadNotice =
  "verifying" | "verified" | "rejected" | "failed" | "pending";

export function useDocumentUpload({ onCompleted }: UseDocumentUploadOptions) {
  const organizationId = useAppSelector(selectCurrentOrganizationId);
  const dispatch = useAppDispatch();
  const switchingLock = useRef<string | null>(null);
  const [file, setFile] = useState<File | null>(null);
  const [storageUploadError, setStorageUploadError] = useState<unknown>();
  const [isUploadingToStorage, setIsUploadingToStorage] = useState(false);
  const [documentId, setDocumentId] = useState<string>();
  const [pollingExpired, setPollingExpired] = useState(false);
  const [notice, setNotice] = useState<DocumentUploadNotice>();
  const uploadController = useRef<AbortController | null>(null);
  const mounted = useRef(true);
  const [
    beginDocumentUpload,
    { error: beginUploadError, isLoading: isBeginningUpload },
  ] = useBeginDocumentUploadMutation();

  const { currentData: document } = useGetDocumentQuery(
    { documentId: documentId ?? "", "X-Organization-Id": organizationId ?? "" },
    {
      skip:
        !documentId ||
        !organizationId ||
        pollingExpired ||
        notice !== "verifying",
      pollingInterval: DOCUMENT_POLL_INTERVAL_MS,
      refetchOnMountOrArgChange: true,
    },
  );

  useEffect(() => {
    mounted.current = true;

    return () => {
      mounted.current = false;
      uploadController.current?.abort();
      if (switchingLock.current) {
        dispatch(unlockOrganizationSwitching(switchingLock.current));
        switchingLock.current = null;
      }
    };
  }, [dispatch]);

  useEffect(() => {
    // Verification belongs to the previous Organization; its server work continues.
    setDocumentId(undefined);
    setNotice(undefined);
    setStorageUploadError(undefined);
    setFile(null);
  }, [organizationId]);

  useEffect(() => {
    if (!isUploadingToStorage) return;

    function warnBeforeLeaving(event: BeforeUnloadEvent) {
      event.preventDefault();
      event.returnValue = true;
    }

    window.addEventListener("beforeunload", warnBeforeLeaving);

    return () => window.removeEventListener("beforeunload", warnBeforeLeaving);
  }, [isUploadingToStorage]);

  useEffect(() => {
    if (!documentId || notice !== "verifying") return;

    const timer = window.setTimeout(() => {
      setPollingExpired(true);
      setNotice("pending"); // Server verification continues; timeout is NOT upload failure.
    }, DOCUMENT_POLL_DURATION_MS);

    return () => window.clearTimeout(timer);
  }, [documentId, notice]);

  useEffect(() => {
    if (
      !document ||
      document.id !== documentId ||
      document.status === "PendingForUpload"
    ) {
      return;
    }

    setNotice(
      document.status === "Rejected"
        ? "rejected"
        : document.status === "Failed"
          ? "failed"
          : "verified",
    );

    setDocumentId(undefined);
    onCompleted();
  }, [document, documentId, onCompleted]);

  async function uploadSelectedFile() {
    if (
      !file ||
      !organizationId ||
      switchingLock.current ||
      isBeginningUpload ||
      isUploadingToStorage
    ) {
      return;
    }

    setStorageUploadError(undefined);
    setNotice(undefined);
    setDocumentId(undefined);

    const lockId = crypto.randomUUID();
    switchingLock.current = lockId;
    dispatch(lockOrganizationSwitching(lockId));

    try {
      const reservation = await beginDocumentUpload({
        "X-Organization-Id": organizationId,
        createDocumentUploadRequest: { fileName: file.name },
      });

      if (!mounted.current || "error" in reservation) {
        return;
      }

      const controller = new AbortController();
      uploadController.current = controller;
      setIsUploadingToStorage(true);

      try {
        // A single Put Blob works with sp=c. Do not replace with chunked Put Block/Put Block List.
        const response = await fetch(reservation.data.uploadUri, {
          method: "PUT",
          headers: {
            "If-None-Match": "*",
            "x-ms-blob-content-type": reservation.data.contentType,
            "x-ms-blob-type": "BlockBlob",
            "x-ms-version": "2023-11-03",
          },
          body: file,
          signal: controller.signal,
        });
        if (!mounted.current) return;
        if (!response.ok) {
          setStorageUploadError({
            data: {
              message: `Storage upload failed with status ${response.status}.`,
            },
          });
          return;
        }

        setFile(null);
        setDocumentId(reservation.data.documentId);
        setPollingExpired(false);
        setNotice("verifying");
        onCompleted();
      } catch {
        if (mounted.current && !controller.signal.aborted)
          setStorageUploadError({
            data: {
              message:
                "The browser could not confirm the upload. Check the document list before trying again.",
            },
          });
      } finally {
        if (uploadController.current === controller) {
          uploadController.current = null;
        }

        if (mounted.current) {
          setIsUploadingToStorage(false);
        }
      }
    } finally {
      dispatch(unlockOrganizationSwitching(lockId));
      if (switchingLock.current === lockId) {
        switchingLock.current = null;
      }
    }
  }

  return {
    file,
    setFile,
    notice,
    isUploading: isBeginningUpload || isUploadingToStorage,
    error: storageUploadError ?? beginUploadError,
    uploadSelectedFile,
    retryUpload: () => {
      void uploadSelectedFile();
    },
  };
}
