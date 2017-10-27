
#include "RenderAPI.h"
#include "PlatformBase.h"


// Metal implementation of RenderAPI.


#if SUPPORT_METAL

#include "../UnityPluginAPI/IUnityGraphicsMetal.h"
#import <Metal/Metal.h>


class RenderAPI_Metal : public RenderAPI
{
public:
	RenderAPI_Metal();
	virtual ~RenderAPI_Metal() { }
	
	virtual void ProcessDeviceEvent(UnityGfxDeviceEventType type, IUnityInterfaces* interfaces) { }
		
	virtual void* BeginModifyTexture(void* textureHandle, int textureWidth, int textureHeight, int* outRowPitch);
	virtual void EndModifyTexture(void* textureHandle, int textureWidth, int textureHeight, int rowPitch, void* dataPtr);
	
};


RenderAPI* CreateRenderAPI_Metal()
{
	return new RenderAPI_Metal();
}


void* RenderAPI_Metal::BeginModifyTexture(void* textureHandle, int textureWidth, int textureHeight, int* outRowPitch)
{
	const int rowPitch = textureWidth * 3;
	// Just allocate a system memory buffer here for simplicity
	unsigned char* data = new unsigned char[rowPitch * textureHeight];
	*outRowPitch = rowPitch;
	return data;
}


void RenderAPI_Metal::EndModifyTexture(void* textureHandle, int textureWidth, int textureHeight, int rowPitch, void* dataPtr)
{
	id<MTLTexture> tex = (__bridge id<MTLTexture>)textureHandle;
	// Update texture data, and free the memory buffer
	[tex replaceRegion:MTLRegionMake3D(0,0,0, textureWidth,textureHeight,1) mipmapLevel:0 withBytes:dataPtr bytesPerRow:rowPitch];
	delete[](unsigned char*)dataPtr;
}


#endif // #if SUPPORT_METAL
