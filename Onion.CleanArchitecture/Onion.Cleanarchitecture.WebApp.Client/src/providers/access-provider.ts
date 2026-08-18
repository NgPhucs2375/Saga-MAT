import { AccessControlProvider } from "@refinedev/core";
import { authProvider } from "./auth-provider";
import { Roles } from "./types";

export const accessControlProvider: AccessControlProvider = {
  // ham check quyền truy cập dựa trên resource và action 
  can: async ({ resource, action }) => {
    // check
    if (!authProvider || typeof authProvider.getPermissions !== "function") {
      return {
        can: false,
        reason: "AuthProvider hoặc getPermissions chưa được xác định.",
      };
    }

    // lấy thông tin quyền từ authProvider và đặt tên là Roles
    const roles = JSON.parse(
      (await authProvider.getPermissions()) as string
    ) as Roles;

    // Destructuring Assignment (Phân rã đối tượng)
    // nếu const permissions = roles là gán all roles vào hằng đó
    // còn {permissions} đó nghĩa là thò tay vào và lấy duy nhất thuộc tính permissions trong roles không đụng tới những property khác
    const { permissions } = roles;


      for (const permission of permissions) {
        if (
          permission.resource === resource &&
          permission.action.includes(action)
        ) {
          return { can: true };
        }
      }

    return {
      can: false,
      reason: "Unauthorized",
    };
  },
  options: {
    buttons: {
      enableAccessControl: true,
      hideIfUnauthorized: true,
    },
  },
};
