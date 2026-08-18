import { App as AntdApp, ConfigProvider } from "antd";
import React from "react";
import {
  AuthPage,
  ErrorComponent,
  ImageField,
  ThemedLayoutV2,
  ThemedTitleV2,
  useNotificationProvider,
} from "@refinedev/antd";
import routerProvider, {
  CatchAllNavigate,
  NavigateToResource,
} from "@refinedev/react-router-v6";
import { BrowserRouter, Outlet, Route, Routes } from "react-router-dom";
import { resources, appTheme } from "./config";
import { Authenticated, CanAccess, Refine } from "@refinedev/core";
import { accessControlProvider, authProvider, dataProvider } from "./providers";
import {
  CloneUser,
  CreateUser,
  EditUser,
  ListUser,
  ShowUser,
} from "@routes/identity/users";
import {
  CloneProduct,
  CreateProduct,
  CreateRangeProduct,
  EditProduct,
  ListProduct,
  ShowProduct,
} from "@routes/products";
import {
  CloneRole,
  CreateRole,
  EditRole,
  ListRole,
  ShowRole,
} from "@routes/roles";
import {
  CloneRoleClaim,
  CreateRoleClaim,
  EditRoleClaim,
  ListRoleClaim,
  ShowRoleClaim,
} from "@routes/roleclaims";
import { Dashboard } from "@routes/dashboards";
import { ListOrder } from "@routes/orders";
import { CreateOrder } from "./routes/orders/create";
import { Unauthorized } from "@components/unauthorized";
import { Header } from "@components/header";
import { NotificationProvider } from "@components/notifications/notification-context";
import { ShowOrder } from "./routes/orders/show";
import { EditOrder } from "./routes/orders/edit"; 
import { ListNoti, ShowNoti } from "@routes/notifications";

const App: React.FC = () => {
  return (
    <BrowserRouter>
      <ConfigProvider theme={appTheme}>
        <AntdApp>
          <Refine // Center điều phối các provider
            dataProvider={dataProvider} // Cách gọi API
            authProvider={authProvider} // đăng nhập/identity
            routerProvider={routerProvider} // Đồng bộ định tuyến của React Router v6 với resource nội bộ Refine
            accessControlProvider={accessControlProvider} // phân quyền
            notificationProvider={useNotificationProvider} // toast
            options={{
              syncWithLocation: true,
              warnWhenUnsavedChanges: true,
            }}
            resources={resources}
          >
            <NotificationProvider>
            <Routes>
              <Route
                element={
                  <Authenticated  // Kiểm tra xác thực
                    key="authenticated-routes"
                    fallback={<CatchAllNavigate to="/login" />}
                  >
                    <ThemedLayoutV2  // layout
                      Header={Header}
                      Title={({ collapsed }: { collapsed: boolean }) => (
                        <ThemedTitleV2
                          collapsed={collapsed}
                          icon={
                            <ImageField
                              value="/logojack.jpg"
                              title="Logo"
                              style={{width: 28, height: 28, objectFit: "contain" }}
                            />
                          }
                          text="Admin"
                        />
                      )}
                    >


                      
                      <Outlet /> // render các route con tương ứng 
                    </ThemedLayoutV2>
                  </Authenticated>
                }
              >
                <Route
                  index
                  element={<NavigateToResource resource="dashboard" />}
                />
                <Route path="dashboard">
                  <Route
                    index
                    element={
                      <CanAccess // tự động gửi YC check đến accessProvider xem user có quyền truy cập resource/action hay không
                        resource="dashboard"
                        action="list"
                        fallback={<Unauthorized />}
                      >
                        <Dashboard />
                      </CanAccess>
                    }
                  />
                </Route>
                <Route path="products">
                  <Route
                    index
                    element={
                      <CanAccess
                        resource="products"
                        action="list"
                        fallback={<Unauthorized />}
                      >
                        <ListProduct />
                      </CanAccess>
                    }
                  />
                  <Route
                    path="create"
                    element={
                      <CanAccess
                        resource="products"
                        action="create"
                        fallback={<Unauthorized />}
                      >
                        <CreateProduct />
                      </CanAccess>
                    }
                  />
                  <Route
                    path="create-range"
                    element={
                      <CanAccess
                        resource="products"
                        action="create-range"
                        fallback={<Unauthorized />}
                      >
                        <CreateRangeProduct />
                      </CanAccess>
                    }
                  />
                  <Route
                    path=":id/clone"
                    element={
                      <CanAccess
                        resource="products"
                        action="clone"
                        fallback={<Unauthorized />}
                      >
                        <CloneProduct />
                      </CanAccess>
                    }
                  />
                  <Route
                    path=":id/edit"
                    element={
                      <CanAccess
                        resource="products"
                        action="edit"
                        fallback={<Unauthorized />}
                      >
                        <EditProduct />
                      </CanAccess>
                    }
                  />
                  <Route
                    path=":id"
                    element={
                      <CanAccess
                        resource="products"
                        action="show"
                        fallback={<Unauthorized />}
                      >
                        <ShowProduct />
                      </CanAccess>
                    }
                  />
                </Route>
                <Route path="orders">
                  <Route
                    index
                    element={
                      <CanAccess
                        resource="orders"
                        action="list"
                        fallback={<Unauthorized />}
                      >
                        <ListOrder />
                      </CanAccess>
                    }
                  />
                  <Route
                    path="create"
                    element={
                      <CanAccess
                        resource="orders"
                        action="create"
                        fallback={<Unauthorized />}
                      >
                        <CreateOrder />
                      </CanAccess>
                    }
                  />
                  <Route
                    path=":id/edit"
                    element={
                      <CanAccess
                        resource="orders"
                        action="edit"
                        fallback={<Unauthorized />}
                      >
                        <EditOrder />
                      </CanAccess>
                    }
                  />
                  <Route
                    path=":id"
                    element={
                      <CanAccess
                        resource="orders"
                        action="show"
                        fallback={<Unauthorized />}
                      >
                        <ShowOrder />
                      </CanAccess>
                    }
                  />
                </Route>

                <Route path="users">
                  <Route
                    index
                    element={
                      <CanAccess
                        resource="users"
                        action="list"
                        fallback={<Unauthorized />}
                      >
                        <ListUser />
                      </CanAccess>
                    }
                  />
                  <Route
                    path="create"
                    element={
                      <CanAccess
                        resource="users"
                        action="create"
                        fallback={<Unauthorized />}
                      >
                        <CreateUser />
                      </CanAccess>
                    }
                  />
                  <Route
                    path=":id/clone"
                    element={
                      <CanAccess
                        resource="users"
                        action="clone"
                        fallback={<Unauthorized />}
                      >
                        <CloneUser />
                      </CanAccess>
                    }
                  />
                  <Route
                    path=":id/edit"
                    element={
                      <CanAccess
                        resource="users"
                        action="edit"
                        fallback={<Unauthorized />}
                      >
                        <EditUser />
                      </CanAccess>
                    }
                  />
                  <Route
                    path=":id"
                    element={
                      <CanAccess
                        resource="users"
                        action="show"
                        fallback={<Unauthorized />}
                      >
                        <ShowUser />
                      </CanAccess>
                    }
                  />
                </Route>
                <Route path="roles">
                  <Route
                    index
                    element={
                      <CanAccess
                        resource="roles"
                        action="list"
                        fallback={<Unauthorized />}
                      >
                        <ListRole />
                      </CanAccess>
                    }
                  />
                  <Route
                    path="create"
                    element={
                      <CanAccess
                        resource="roles"
                        action="create"
                        fallback={<Unauthorized />}
                      >
                        <CreateRole />
                      </CanAccess>
                    }
                  />
                  <Route
                    path=":id/clone"
                    element={
                      <CanAccess
                        resource="roles"
                        action="clone"
                        fallback={<Unauthorized />}
                      >
                        <CloneRole />
                      </CanAccess>
                    }
                  />
                  <Route
                    path=":id/edit"
                    element={
                      <CanAccess
                        resource="roles"
                        action="edit"
                        fallback={<Unauthorized />}
                      >
                        <EditRole />
                      </CanAccess>
                    }
                  />
                  <Route
                    path=":id"
                    element={
                      <CanAccess
                        resource="roles"
                        action="show"
                        fallback={<Unauthorized />}
                      >
                        <ShowRole />
                      </CanAccess>
                    }
                  />
                </Route>
                <Route path="notifications">
                  <Route
                    index
                    element={
                      <CanAccess
                        resource="notifications"
                        action="list"
                        fallback={<Unauthorized />}
                      >
                        <ListNoti />
                      </CanAccess>
                    }
                  />
                  <Route
                    path=":id"
                    element={
                      <CanAccess
                        resource="notifications"
                        action="show"
                        fallback={<Unauthorized />}
                      >
                        <ShowNoti />
                      </CanAccess>
                    }
                  />
                </Route>

                <Route path="roleclaims">
                  <Route
                    index
                    element={
                      <CanAccess
                        resource="roleclaims"
                        action="list"
                        fallback={<Unauthorized />}
                      >
                        <ListRoleClaim />
                      </CanAccess>
                    }
                  />
                  <Route
                    path="create"
                    element={
                      <CanAccess
                        resource="roleclaims"
                        action="create"
                        fallback={<Unauthorized />}
                      >
                        <CreateRoleClaim />
                      </CanAccess>
                    }
                  />

                  <Route
                    path=":id/clone"
                    element={
                      <CanAccess
                        resource="roleclaims"
                        action="clone"
                        fallback={<Unauthorized />}
                      >
                        <CloneRoleClaim />
                      </CanAccess>
                    }
                  />
                  <Route
                    path=":id/edit"
                    element={
                      <CanAccess
                        resource="roleclaims"
                        action="edit"
                        fallback={<Unauthorized />}
                      >
                        <EditRoleClaim />
                      </CanAccess>
                    }
                  />
                  <Route
                    path=":id"
                    element={
                      <CanAccess
                        resource="roleclaims"
                        action="show"
                        fallback={<Unauthorized />}
                      >
                        <ShowRoleClaim />
                      </CanAccess>
                    }
                  />
                </Route>
              </Route>
              <Route
                element={
                  <Authenticated key="auth-pages" fallback={<Outlet />}>
                    <NavigateToResource />
                  </Authenticated>
                }
              >
                <Route
                  path="/login"
                  element={
                    <AuthPage
                      type="login"
                      title={
                        <ThemedTitleV2
                          icon={
                            <ImageField
                              value="/logojack.jpg"
                              title="Logo"
                              style={{ width: 30, height: 30 }}
                            />
                          }
                          text="Admin"
                          collapsed={false}
                        />
                      }
                      forgotPasswordLink={true}
                      registerLink={true}
                      formProps={{
                        initialValues: {
                          email: "",
                          password: "",
                        },
                      }}
                    />
                  }
                />
              </Route>
              <Route
                element={
                  <Authenticated key="catch-all">
                    <ThemedLayoutV2>
                      <Outlet />
                    </ThemedLayoutV2>
                  </Authenticated>
                }
              >
                <Route path="*" element={<ErrorComponent />} />
              </Route>
            </Routes>
            </NotificationProvider>
          </Refine>
        </AntdApp>
      </ConfigProvider>
    </BrowserRouter>
  );
};

export default App;
