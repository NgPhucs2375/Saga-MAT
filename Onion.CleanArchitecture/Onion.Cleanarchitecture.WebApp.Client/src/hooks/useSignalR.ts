import { useEffect } from "react";
import { useGetIdentity } from "@refinedev/core";
import { signalRService } from "@providers/signalr-provider";
import { notification } from "antd";

export const useSignalR = () => {
  const { data: user } = useGetIdentity<{ Uid: string }>();

  useEffect(() => {
    if (!user?.Uid) return;

    signalRService.start(user.Uid);

    const unsubscribe = signalRService.onNotification((noti) => {
      notification.info({
        message: noti.Title,
        description: noti.Message,
        placement: "topRight",
      });
    });

    return () => {
      unsubscribe();
      signalRService.stop();
    };
  }, [user?.Uid]);
};
