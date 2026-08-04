import { useEffect } from "react";
import { useGetIdentity } from "@refinedev/core";
import { signalRService } from "@providers/signalr-provider";
import { notification } from "antd";

export const useSignalR = () => {
  const { data: user } = useGetIdentity<{ id: string }>();

  useEffect(() => {
    if (!user?.id) return;

    signalRService.start(user.id);

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
  }, [user?.id]);
};
