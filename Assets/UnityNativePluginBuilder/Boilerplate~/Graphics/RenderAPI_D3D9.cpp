#include "RenderAPI.h"
#include "PlatformBase.h"

// Direct3D 9 implementation of RenderAPI.

#if SUPPORT_D3D9

#include <assert.h>
#include <d3d9.h>
#include "../UnityPluginAPI/IUnityGraphicsD3D9.h"


class RenderAPI_D3D9 : public RenderAPI
{
public:
	RenderAPI_D3D9();
	virtual ~RenderAPI_D3D9() { }

	virtual void ProcessDeviceEvent(UnityGfxDeviceEventType type, IUnityInterfaces* interfaces) { }

	virtual void* BeginModifyTexture(void* textureHandle, int textureWidth, int textureHeight, int* outRowPitch);
	virtual void EndModifyTexture(void* textureHandle, int textureWidth, int textureHeight, int rowPitch, void* dataPtr);

};


RenderAPI* CreateRenderAPI_D3D9()
{
	return new RenderAPI_D3D9();
}


void* RenderAPI_D3D9::BeginModifyTexture(void* textureHandle, int textureWidth, int textureHeight, int* outRowPitch)
{
	IDirect3DTexture9* d3dtex = (IDirect3DTexture9*)textureHandle;
	assert(d3dtex);

	// Lock the texture and return pointer
	D3DLOCKED_RECT lr;
	HRESULT hr = d3dtex->LockRect(0, &lr, NULL, 0);
	if (FAILED(hr))
		return NULL;

	*outRowPitch = lr.Pitch;
	return lr.pBits;
}


void RenderAPI_D3D9::EndModifyTexture(void* textureHandle, int textureWidth, int textureHeight, int rowPitch, void* dataPtr)
{
	IDirect3DTexture9* d3dtex = (IDirect3DTexture9*)textureHandle;
	assert(d3dtex);

	// Unlock the texture after modification
	d3dtex->UnlockRect(0);
}


#endif // #if SUPPORT_D3D9
