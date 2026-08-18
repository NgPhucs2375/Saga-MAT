import { AuthProvider as BaseAuthProvider, HttpError } from "@refinedev/core";
import { jwtDecode } from "jwt-decode";
import { ResponseRoot } from "./types";
interface ResponseAuthen {
  Succeeded: boolean;
  Message: string;
  Errors: any;
  Data: any;
}

interface RefreshTokenResponse {
  AccessToken: string;
  RefreshToken: string;
}

interface AuthProvider extends BaseAuthProvider {
  refresh: () => Promise<{ success: boolean }>;
}

export const authProvider: AuthProvider = {
  // kiểm tra xem người dùng đã đăng nhập hay chưa
  check: async () => {
    // lấy token từ localStorage
    const token = localStorage.getItem("access_token");
    // nếu token tồn tại thì trả về authenticated: true, ngược lại trả về authenticated: false và thông báo lỗi
    if (Boolean(token)) {
      return { authenticated: true };
    }
    return {
      authenticated: false,
      error: {
        message: "Kiểm tra thất bại",
        name: "Không có được xác thực",
      },
      logout: true,
      redirectTo: "/login",
    };
  },

  // Lấy thông tin định danh của người dùng từ API
  getIdentity: async () => {
    const response = await fetch("/api/account/me", {
      headers: {
        Authorization: `Bearer ${localStorage.getItem("access_token") ?? ""}`,
      },
    });

    const data = (await response.json()) as ResponseRoot;

    if (response.status < 200 || response.status > 299 || !data.Succeeded) {
      localStorage.removeItem("access_token");
      const error: HttpError = {
        message: data.Message ?? "Unauthorized",
        statusCode: data.Code ?? response.status,
      };
      return Promise.reject(error);
    }

    return data.Data as any;
  },

  // đăng nhập bằng email và password, nếu thành công thì lưu token vào localStorage
  login: async ({ email, password }) => {
    const response = await fetch("/api/account/authenticate", {
      method: "POST",
      body: JSON.stringify({ Email: email, Password: password }),
      headers: {
        "Content-Type": "application/json",
      },
    });

    const data = (await response.json()) as ResponseAuthen;
    console.log(data);
    if (data.Succeeded) {
      if (data.Data.JWToken) {
        localStorage.setItem("access_token", data.Data.JWToken);
        // localStorage.setItem("refresh_token", data.Data.RefreshToken);
        return {
          success: true,
          successNotification: {
            message: "Login Successful",
            description: "You have been successfully logged in.",
          },
          redirectTo: "/dashboard",
        };
      }
    }

    return {
      success: false,
      error: {
        name: "Login Failed!",
        message: data.Message ?? "Invalid email or password",
      },
    };
  },
  // đăng xuất bằng cách xóa token khỏi localStorage
  logout: async () => {
    localStorage.removeItem("access_token");
    localStorage.removeItem("refresh_token");

    // We're returning success: true to indicate that the logout operation was successful.
    return { success: true };
  },
  // xử lý lỗi khi gọi API, nếu lỗi là 401 thì xóa token khỏi localStorage và trả về error
  onError: async (error) => {
    if (error.statusCode === 401) {
      localStorage.removeItem("access_token");
      localStorage.removeItem("refresh_token");
    }
    return { error };
  },

  // lấy thông tin quyền của người dùng từ token JWT, nếu không có token thì trả về mảng rỗng
  getPermissions: async () => {
    // lấy token từ localStorage
      const token = localStorage.getItem("access_token");
      // nếu không có token thì trả về mảng rỗng
      if (!token) {
        return JSON.stringify({ permissions: [] });
      }

      
      try {
        // 
        const decoded: any = jwtDecode(token);

        // Đọc claim roles động từ JWT
        const rolesClaim = decoded.roles ?? decoded.role ?? [];
        
        // Chuẩn hóa roles: hỗ trợ cả chuỗi đơn lẻ (string) lẫn mảng (array)
        const rolesArray = Array.isArray(rolesClaim)
          ? rolesClaim
          : typeof rolesClaim === "string"
          ? [rolesClaim]
          : [];

        const permissionsList: { resource: string; action: string }[] = [];

        // Lặp qua danh sách roles thu thập từ JWT
        rolesArray.forEach((roleItem: any) => {
          let roleObj = roleItem;

          // Nếu roleItem ở dạng JSON string thì parse sang Object
          if (typeof roleItem === "string") {
            try {
              roleObj = JSON.parse(roleItem);
            } catch {
              return; // Bỏ qua nếu là chuỗi thuần không phải JSON cấu trúc
            }
          }

          // Kiểm tra tính hợp lệ của mảng permissions trong object role
          if (roleObj && Array.isArray(roleObj.permissions)) {
            roleObj.permissions.forEach((p: { resource: string; action: string[] }) => {
              if (p.resource && Array.isArray(p.action)) {
                p.action.forEach((act: string) => {
                  // Thêm vào mảng permissions nếu chưa tồn tại (tránh trùng lặp khi gộp nhiều role)
                  const exists = permissionsList.some(
                    (item) => item.resource === p.resource && item.action === act
                  );
                  if (!exists) {
                    permissionsList.push({ resource: p.resource, action: act });
                  }
                });
              }
            });
          }
        });

        return JSON.stringify({ permissions: permissionsList });
      } catch (error) {
        console.error("Lỗi khi giải mã JWT hoặc đọc permissions:", error);
        return JSON.stringify({ permissions: [] });
      }
    },

  // Cập nhật mật khẩu bằng cách gọi API, nếu thành công thì trả về success: true, ngược lại trả về success: false và thông báo lỗi
  updatePassword: async ({ oldPassword, newPassword }) => {
    const response = await fetch("/api/account/update-password", {
      method: "POST",
      body: JSON.stringify({ oldPassword, newPassword }),
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${localStorage.getItem("access_token") ?? ""}`,
      },
    });

    const data = (await response.json()) as ResponseRoot;
    if (data.Succeeded) {
      return {
        success: true,
        successNotification: {
          message: "Password Updated",
          description: "Your password has been successfully updated.",
        },
        redirectTo: "/dashboard",
      };
    }
    return {
      success: false,
      error: {
        name: "Login Failed!",
        message: data.Message ?? "Invalid email or password",
      },
    };
  },
  // làm mới token bằng cách gọi API, nếu thành công thì lưu token mới vào localStorage, ngược lại xóa token khỏi localStorage
  refresh: async () => {
    const refreshToken = localStorage.getItem("refresh_token");
    const accessToken = localStorage.getItem("access_token");
    const response = await fetch("/api/account/refresh-token", {
      method: "POST",
      body: JSON.stringify({
        AccessToken: accessToken,
        RefreshToken: refreshToken,
      }),
      headers: {
        "Content-Type": "application/json",
      },
    });
    if (response.ok) {
      const data = (await response.json()) as RefreshTokenResponse;
      localStorage.setItem("access_token", data.AccessToken);
      localStorage.setItem("refresh_token", data.RefreshToken);
      return { success: true };
    } else {
      localStorage.removeItem("access_token");
      localStorage.removeItem("refresh_token");
      return { success: false };
    }
  },
};
