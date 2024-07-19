from manim import *
DEFAULT_FONT_SIZE = 40

Text.set_default(font="sans-serif", font_size=DEFAULT_FONT_SIZE)

class SurplusCalculation(Scene):
    def construct(self):
        self.camera.background_color = "#444444"

        # equationStart = Tex(r"Total cost", r"=", "fare", r"+ (Time to destination \cdot Cost of time)", font_size=DEFAULT_FONT_SIZE).move_to(DOWN*2)
        equationStart = Tex(r"Welfare gained", r" = Total cost of Best Substitute", r" - Total cost of Uber", tex_template=TexFontTemplates.droid_sans ,font_size=DEFAULT_FONT_SIZE)
        # equationWithValues = MathTex(r"= \$12.00", r"+ 0.35 \, hrs", r"× \$141.20/hr", font_size=DEFAULT_FONT_SIZE)
        equationWithValues = Tex(r"= \$105.50", r" - \$68.67", tex_template=TexFontTemplates.droid_sans, font_size=DEFAULT_FONT_SIZE)
        equationWithValues.next_to(equationStart, DOWN).align_to(equationStart[1], LEFT)
        equationResult = Tex(r"= \$36.83", tex_template=TexFontTemplates.droid_sans, font_size=DEFAULT_FONT_SIZE)
        equationResult.next_to(equationWithValues, DOWN).align_to(equationStart[1], LEFT)

        self.play(GrowFromCenter(equationStart[0]))
        self.wait(1)
        self.play(GrowFromEdge(equationStart[1], LEFT))
        self.wait(1)
        self.play(GrowFromEdge(equationStart[2], LEFT))
        
        self.wait(1)
        self.play(LaggedStart(
            TransformFromCopy(equationStart[1], equationWithValues[0]),
            TransformFromCopy(equationStart[2], equationWithValues[1]),
            lag_ratio=0.1,
            run_time=1
        ))
        self.wait(1)

        self.play(TransformFromCopy(equationWithValues, equationResult))
        self.wait(3)
