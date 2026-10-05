const RAW_API_URL =
  import.meta.env.VITE_API_URL ||
  "http://localhost:5198";


const API_ORIGIN =
  RAW_API_URL
    .replace(/\/+$/, "")
    .replace(/\/api$/, "");


// ============================================================
// TOKEN
// ============================================================

function getToken() {
  const token =
    localStorage.getItem(
      "gemora_token"
    ) ||
    sessionStorage.getItem(
      "gemora_token"
    );


  if (!token) {
    return "";
  }


  return token
    .replace(
      /^Bearer\s+/i,
      ""
    )
    .replace(
      /^"(.*)"$/,
      "$1"
    )
    .trim();
}


// ============================================================
// REQUEST
// ============================================================

async function request(
  path,
  options = {},
  authenticated = true
) {
  const headers = {
    ...(options.body
      ? {
          "Content-Type":
            "application/json",
        }
      : {}),

    ...(options.headers || {}),
  };


  if (authenticated) {
    const token =
      getToken();


    if (token) {
      headers.Authorization =
        `Bearer ${token}`;
    }
  }


  const url =
    `${API_ORIGIN}${path}`;


  const response =
    await fetch(
      url,
      {
        ...options,
        headers,
      }
    );


  if (!response.ok) {
    let message =
      `Request failed (${response.status}).`;


    try {
      const contentType =
        response.headers.get(
          "content-type"
        );


      if (
        contentType?.includes(
          "application/json"
        )
      ) {
        const body =
          await response.json();


        message =
          body?.message ||
          body?.detail ||
          body?.title ||
          body?.error ||
          message;
      } else {
        const responseText =
          await response.text();


        if (responseText) {
          message =
            responseText;
        }
      }
    } catch (error) {
      console.error(
        "Unable to parse API error response:",
        error
      );
    }


    if (
      response.status === 401
    ) {
      message =
        "Your login session is invalid or expired. Please sign in again.";
    }


    console.error(
      "Gemora API request failed:",
      {
        url,
        status:
          response.status,
        message,
      }
    );


    throw new Error(
      message
    );
  }


  if (
    response.status === 204
  ) {
    return null;
  }


  const contentType =
    response.headers.get(
      "content-type"
    );


  if (
    contentType?.includes(
      "application/json"
    )
  ) {
    return await response.json();
  }


  return null;
}


// ============================================================
// MEDIA
// ============================================================

export function resolveMediaUrl(
  path
) {
  if (!path) {
    return null;
  }


  if (
    path.startsWith(
      "http://"
    ) ||
    path.startsWith(
      "https://"
    )
  ) {
    return path;
  }


  const normalizedPath =
    path.startsWith("/")
      ? path
      : `/${path}`;


  return `${API_ORIGIN}${normalizedPath}`;
}


// ============================================================
// MARKETPLACE
// ============================================================

export function getMarketplaceGems({
  search = "",
  page = 1,
  pageSize = 12,
  sort = "",
} = {}) {
  const params =
    new URLSearchParams();


  if (search.trim()) {
    params.set(
      "search",
      search.trim()
    );
  }


  params.set(
    "page",
    String(page)
  );


  params.set(
    "pageSize",
    String(pageSize)
  );


  if (sort) {
    params.set(
      "sort",
      sort
    );
  }


  return request(
    `/api/marketplace/gems?${params.toString()}`,
    {},
    false
  );
}


export async function
  getAllMarketplaceGems() {
  const firstPage =
    await getMarketplaceGems({
      page: 1,
      pageSize: 12,
    });


  const allItems = [
    ...(firstPage?.items || []),
  ];


  const totalPages =
    firstPage?.totalPages || 1;


  for (
    let page = 2;
    page <= totalPages;
    page++
  ) {
    const response =
      await getMarketplaceGems({
        page,
        pageSize: 12,
      });


    allItems.push(
      ...(response?.items || [])
    );
  }


  return {
    items:
      allItems,

    totalItems:
      firstPage
        ?.totalItems ??
      allItems.length,

    totalPages,
  };
}


export function
  getMarketplaceGemById(id) {
  return request(
    `/api/marketplace/gems/${id}`,
    {},
    false
  );
}


export function
  getBuyerDashboardStats() {
  return request(
    "/api/marketplace/stats",
    {},
    false
  );
}


// ============================================================
// ORDERS
// ============================================================

export function getMyOrders() {
  return request(
    "/api/orders/my",
    {},
    true
  );
}


export function getOrderById(
  orderId
) {
  return request(
    `/api/orders/${orderId}`,
    {},
    true
  );
}


export function createOrder({
  gemListingId,
  deliveryDetails,
}) {
  return request(
    "/api/orders",
    {
      method:
        "POST",

      body:
        JSON.stringify({
          gemListingId,
          deliveryDetails,
        }),
    },
    true
  );
}


// ============================================================
// DELIVERY
// ============================================================

export function
  updateOrderDeliveryDetails(
    orderId,
    deliveryDetails
  ) {
  return request(
    `/api/orders/${orderId}/delivery-details`,
    {
      method:
        "PATCH",

      body:
        JSON.stringify(
          deliveryDetails
        ),
    },
    true
  );
}


// ============================================================
// PAYMENT
// ============================================================

export function payOrder(
  orderId,
  paymentMethod = "Card"
) {
  return request(
    `/api/orders/${orderId}/payment`,
    {
      method:
        "POST",

      body:
        JSON.stringify({
          paymentMethod,
        }),
    },
    true
  );
}


// ============================================================
// COMPLETE
// ============================================================

export function completeOrder(
  orderId,
  reason =
    "Buyer confirmed successful delivery."
) {
  return request(
    `/api/orders/${orderId}/complete`,
    {
      method:
        "POST",

      body:
        JSON.stringify({
          reason,
        }),
    },
    true
  );
}


export {
  API_ORIGIN,
};